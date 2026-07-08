from __future__ import annotations

from pathlib import Path
from typing import Callable

from PyQt6.QtWidgets import (
    QCheckBox,
    QDialog,
    QFileDialog,
    QFormLayout,
    QHBoxLayout,
    QLabel,
    QLineEdit,
    QPushButton,
    QSpinBox,
    QVBoxLayout,
)

from app_config import AppConfig


class SettingsWindow(QDialog):
    def __init__(self, config: AppConfig, on_save: Callable[[AppConfig], None], on_reset_position: Callable[[], None]) -> None:
        super().__init__()
        self.setWindowTitle("月薪喵 Linux 设置")
        self.config = AppConfig(**config.__dict__)
        self.on_save = on_save
        self.on_reset_position = on_reset_position
        self.resize(460, 260)

        self.scale_box = QSpinBox()
        self.scale_box.setRange(50, 200)
        self.scale_box.setSingleStep(5)
        self.scale_box.setSuffix("%")
        self.scale_box.setValue(self.config.scale_percent)

        self.opacity_box = QSpinBox()
        self.opacity_box.setRange(30, 100)
        self.opacity_box.setSingleStep(5)
        self.opacity_box.setSuffix("%")
        self.opacity_box.setValue(self.config.opacity_percent)

        self.interval_box = QSpinBox()
        self.interval_box.setRange(2, 120)
        self.interval_box.setSuffix(" 秒")
        self.interval_box.setValue(self.config.interval_seconds)

        self.top_box = QCheckBox("始终置顶")
        self.top_box.setChecked(self.config.always_on_top)

        self.gif_dir = QLineEdit(self.config.gif_directory)
        browse_button = QPushButton("浏览")
        browse_button.clicked.connect(self.browse_gif_dir)
        gif_row = QHBoxLayout()
        gif_row.addWidget(self.gif_dir)
        gif_row.addWidget(browse_button)

        form = QFormLayout()
        form.addRow("缩放比例", self.scale_box)
        form.addRow("透明度", self.opacity_box)
        form.addRow("轮播间隔", self.interval_box)
        form.addRow("", self.top_box)
        form.addRow("GIF 目录", gif_row)

        save_button = QPushButton("保存")
        save_button.clicked.connect(self.save)
        cancel_button = QPushButton("取消")
        cancel_button.clicked.connect(self.reject)
        reset_button = QPushButton("重置窗口位置")
        reset_button.clicked.connect(self.on_reset_position)

        buttons = QHBoxLayout()
        buttons.addWidget(reset_button)
        buttons.addStretch(1)
        buttons.addWidget(save_button)
        buttons.addWidget(cancel_button)

        hint = QLabel("提示：Wayland 下置顶、透明或托盘可能受桌面环境限制，X11 体验更稳定。")
        hint.setWordWrap(True)

        layout = QVBoxLayout()
        layout.addLayout(form)
        layout.addWidget(hint)
        layout.addLayout(buttons)
        self.setLayout(layout)

    def browse_gif_dir(self) -> None:
        selected = QFileDialog.getExistingDirectory(self, "选择 GIF 目录", str(Path(self.gif_dir.text()).expanduser()))
        if selected:
            self.gif_dir.setText(selected)

    def save(self) -> None:
        self.config.scale_percent = int(self.scale_box.value())
        self.config.opacity_percent = int(self.opacity_box.value())
        self.config.interval_seconds = int(self.interval_box.value())
        self.config.always_on_top = self.top_box.isChecked()
        self.config.gif_directory = self.gif_dir.text().strip()
        self.on_save(self.config)
        self.accept()
