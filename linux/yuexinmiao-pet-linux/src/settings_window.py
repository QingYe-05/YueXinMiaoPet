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
    QComboBox,
    QVBoxLayout,
)

from app_config import AppConfig
from administrative_service import AdministrativeService, RegionPath


class SettingsWindow(QDialog):
    def __init__(self, config: AppConfig, regions: AdministrativeService, on_save: Callable[[AppConfig], None], on_reset_position: Callable[[], None]) -> None:
        super().__init__()
        self.setWindowTitle("月薪喵 Linux 设置")
        self.config = AppConfig(**config.__dict__)
        self.regions = regions
        self.on_save = on_save
        self.on_reset_position = on_reset_position
        self.resize(540, 430)

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

        self.weather_box = QCheckBox("显示天气挂件（位于 GIF 正上方）")
        self.weather_box.setChecked(self.config.weather_enabled)
        self.weather_box.toggled.connect(self.update_weather_enabled)
        self.province_box = QComboBox()
        self.city_box = QComboBox()
        self.county_box = QComboBox()
        self.province_box.currentIndexChanged.connect(self.province_changed)
        self.city_box.currentIndexChanged.connect(self.city_changed)
        self.region_search = QLineEdit()
        self.region_search.setPlaceholderText("搜索省、市、县区，例如：金水")
        self.search_results = QComboBox()
        self.region_search.textChanged.connect(self.search_regions)
        self.search_results.activated.connect(self.apply_search_result)
        self.load_regions()

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
        form.addRow("", self.weather_box)
        form.addRow("省 / 直辖市", self.province_box)
        form.addRow("市 / 州 / 盟", self.city_box)
        form.addRow("县 / 区", self.county_box)
        form.addRow("地区搜索", self.region_search)
        form.addRow("搜索结果", self.search_results)

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
        self.update_weather_enabled(self.weather_box.isChecked())

    def load_regions(self) -> None:
        self.province_box.blockSignals(True)
        for region in self.regions.provinces:
            self.province_box.addItem(region.name, region.code)
        index = self.province_box.findData(self.config.province_code)
        self.province_box.setCurrentIndex(max(0, index))
        self.province_box.blockSignals(False)
        self.province_changed()
        city_index = self.city_box.findData(self.config.city_code)
        if city_index >= 0:
            self.city_box.setCurrentIndex(city_index)
            self.city_changed()
        county_index = self.county_box.findData(self.config.county_code)
        if county_index >= 0:
            self.county_box.setCurrentIndex(county_index)

    def province_changed(self) -> None:
        self.city_box.blockSignals(True)
        self.city_box.clear()
        for region in self.regions.cities(str(self.province_box.currentData() or "")):
            self.city_box.addItem(region.name, region.code)
        self.city_box.blockSignals(False)
        self.city_changed()

    def city_changed(self) -> None:
        self.county_box.clear()
        for region in self.regions.counties(str(self.city_box.currentData() or "")):
            self.county_box.addItem(region.name, region.code)

    def search_regions(self, text: str) -> None:
        self.search_results.clear()
        for path in self.regions.search(text):
            self.search_results.addItem(path.display, path)

    def apply_search_result(self, _index: int) -> None:
        path = self.search_results.currentData()
        if not isinstance(path, RegionPath):
            return
        self.province_box.setCurrentIndex(self.province_box.findData(path.province.code))
        self.city_box.setCurrentIndex(self.city_box.findData(path.city.code))
        self.county_box.setCurrentIndex(self.county_box.findData(path.county.code))

    def update_weather_enabled(self, enabled: bool) -> None:
        for widget in (self.province_box, self.city_box, self.county_box, self.region_search, self.search_results):
            widget.setEnabled(enabled)

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
        self.config.weather_enabled = self.weather_box.isChecked()
        self.config.province_code = str(self.province_box.currentData() or "")
        self.config.province_name = self.province_box.currentText()
        self.config.city_code = str(self.city_box.currentData() or "")
        self.config.city_name = self.city_box.currentText()
        self.config.county_code = str(self.county_box.currentData() or "")
        self.config.county_name = self.county_box.currentText()
        self.on_save(self.config)
        self.accept()
