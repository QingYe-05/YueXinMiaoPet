from __future__ import annotations

import logging
from pathlib import Path
from typing import Callable

from PyQt6.QtGui import QAction, QIcon
from PyQt6.QtWidgets import QMenu, QSystemTrayIcon

from app_config import PROJECT_ROOT
from mood_service import MOODS


class TrayService:
    def __init__(
        self,
        parent,
        on_show: Callable[[], None],
        on_mood: Callable[[str], None],
        on_settings: Callable[[], None],
        on_rescan: Callable[[], None],
        on_reset_position: Callable[[], None],
        on_exit: Callable[[], None],
    ) -> None:
        self.tray = None
        if not QSystemTrayIcon.isSystemTrayAvailable():
            logging.warning("当前桌面环境未报告系统托盘可用，Linux 版将无托盘运行。")
            return

        icon = self._load_icon()
        self.tray = QSystemTrayIcon(icon, parent)
        self.tray.setToolTip("月薪喵桌宠")
        menu = QMenu()

        show_action = QAction("显示月薪喵", menu)
        show_action.triggered.connect(on_show)
        menu.addAction(show_action)

        mood_menu = menu.addMenu("今日心情")
        for option in MOODS:
            action = QAction(option.name, mood_menu)
            action.triggered.connect(lambda checked=False, key=option.key: on_mood(key))
            mood_menu.addAction(action)

        menu.addSeparator()
        settings_action = QAction("设置", menu)
        settings_action.triggered.connect(on_settings)
        menu.addAction(settings_action)

        rescan_action = QAction("重新扫描 GIF", menu)
        rescan_action.triggered.connect(on_rescan)
        menu.addAction(rescan_action)

        reset_action = QAction("重置位置", menu)
        reset_action.triggered.connect(on_reset_position)
        menu.addAction(reset_action)

        menu.addSeparator()
        exit_action = QAction("退出", menu)
        exit_action.triggered.connect(on_exit)
        menu.addAction(exit_action)

        self.tray.setContextMenu(menu)
        self.tray.activated.connect(lambda reason: on_show() if reason == QSystemTrayIcon.ActivationReason.DoubleClick else None)
        self.tray.show()

    def _load_icon(self) -> QIcon:
        for name in ("app.png", "tray.png", "app.svg", "tray.svg"):
            path = PROJECT_ROOT / "assets" / "icons" / name
            if path.exists():
                return QIcon(str(path))
        themed = QIcon.fromTheme("face-smile")
        if not themed.isNull():
            return themed
        return QIcon()

    def hide(self) -> None:
        if self.tray is not None:
            self.tray.hide()
