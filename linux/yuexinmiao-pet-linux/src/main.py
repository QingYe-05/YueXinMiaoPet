from __future__ import annotations

import logging
import sys
from pathlib import Path

from PyQt6.QtCore import QPoint, Qt, QTimer
from PyQt6.QtGui import QAction, QMovie
from PyQt6.QtWidgets import QApplication, QLabel, QMenu, QVBoxLayout, QWidget

from app_config import AppConfig, load_config, load_mood_category_map, save_config, setup_logging
from gif_asset_service import GifAssetService
from mood_service import MOODS, mood_name, normalize_mood
from playlist_service import PlaylistService
from settings_window import SettingsWindow
from tray_service import TrayService
from administrative_service import AdministrativeService
from weather_service import WeatherInfo, WeatherService


class PetWindow(QWidget):
    def __init__(self) -> None:
        super().__init__()
        setup_logging()
        logging.info("月薪喵 Linux 版启动")

        self.config = load_config()
        self.mood_map = load_mood_category_map()
        self.asset_service = GifAssetService(self.mood_map)
        self.playlist_service = PlaylistService(self.asset_service)
        self.regions = AdministrativeService()
        self.weather_service = WeatherService()
        self.weather_service.updated.connect(self.weather_updated)
        self.movie = None
        self.drag_start = QPoint()
        self.dragging = False

        self.weather_badge = QLabel()
        self.weather_badge.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self.weather_badge.setMaximumWidth(212)
        self.weather_badge.setWordWrap(False)
        self.weather_badge.setStyleSheet("QLabel { background: rgba(255,255,255,220); color: #5b3b2e; border: 1px solid rgba(122,90,66,170); border-radius: 12px; padding: 5px 10px; font-size: 12px; }")
        self.weather_badge.hide()
        self.label = QLabel()
        self.label.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self.label.setStyleSheet("background: transparent; color: #7a4b32; font-size: 14px;")

        self.timer = QTimer(self)
        self.timer.timeout.connect(self.play_next_gif)
        self.weather_timer = QTimer(self)
        self.weather_timer.timeout.connect(self.refresh_weather)
        self.weather_rotate_timer = QTimer(self)
        self.weather_rotate_timer.setInterval(4000)
        self.weather_rotate_timer.timeout.connect(self.rotate_weather_badge)
        self.weather_temperature_text = ""
        self.weather_wind_text = ""
        self.weather_page = 0

        self.layout = QVBoxLayout(self)
        self.layout.setContentsMargins(0, 0, 0, 0)
        self.layout.setSpacing(6)
        self.layout.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self.layout.addWidget(self.weather_badge, 0, Qt.AlignmentFlag.AlignHCenter)
        self.layout.addWidget(self.label, 1)

        self.init_window()
        self.rescan_gifs()
        self.apply_config()
        self.move(self.config.x, self.config.y)
        self.play_next_gif()
        self.timer.start(self.config.interval_seconds * 1000)
        if self.config.weather_enabled:
            self.refresh_weather()
            self.weather_timer.start(self.config.weather_refresh_minutes * 60 * 1000)

        self.tray = TrayService(
            self,
            on_show=self.show_pet,
            on_mood=self.set_mood,
            on_settings=self.open_settings,
            on_rescan=self.rescan_gifs,
            on_reset_position=self.reset_position,
            on_exit=self.exit_app,
        )

    def init_window(self) -> None:
        flags = Qt.WindowType.FramelessWindowHint | Qt.WindowType.Tool
        if self.config.always_on_top:
            flags |= Qt.WindowType.WindowStaysOnTopHint
        self.setWindowFlags(flags)
        self.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground, True)
        self.setWindowTitle("月薪喵桌宠 Linux")

    def apply_config(self) -> None:
        self.init_window()
        size = max(80, int(220 * self.config.scale_percent / 100))
        badge_height = 34 if self.config.weather_enabled else 0
        self.resize(size, size + badge_height)
        self.label.setFixedSize(size, size)
        self.setWindowOpacity(max(0.3, min(1.0, self.config.opacity_percent / 100)))
        self.timer.setInterval(self.config.interval_seconds * 1000)
        self.weather_timer.setInterval(self.config.weather_refresh_minutes * 60 * 1000)
        if self.config.weather_enabled:
            self.weather_badge.show()
            if not self.weather_timer.isActive():
                self.weather_timer.start()
        else:
            self.weather_badge.hide()
            self.weather_timer.stop()
            self.weather_rotate_timer.stop()

    def rescan_gifs(self) -> None:
        self.asset_service.scan(self.config.gif_directory)
        self.playlist_service.reset()
        logging.info("重新扫描 GIF：%s", self.config.gif_directory)

    def play_next_gif(self) -> None:
        asset = self.playlist_service.next_gif(self.config.mood)
        if asset is None:
            self.label.setText("未找到 GIF\n请同步 assets/classified_gifs")
            return

        self.label.setText("")
        if self.movie is not None:
            self.movie.stop()
            self.movie.deleteLater()

        self.movie = QMovie(str(asset.path))
        self.movie.setCacheMode(QMovie.CacheMode.CacheAll)
        self.movie.setScaledSize(self.label.size())
        self.label.setMovie(self.movie)
        self.movie.start()
        logging.info("播放 GIF：%s mood=%s category=%s", asset.path.name, self.config.mood, asset.category)

    def set_mood(self, mood: str) -> None:
        self.config.mood = normalize_mood(mood)
        save_config(self.config)
        self.playlist_service.reset(self.config.mood)
        self.play_next_gif()
        logging.info("切换心情：%s", self.config.mood)

    def open_settings(self) -> None:
        dialog = SettingsWindow(self.config, self.regions, self.save_settings, self.reset_position)
        dialog.exec()

    def save_settings(self, new_config: AppConfig) -> None:
        self.config = new_config.normalize()
        save_config(self.config)
        self.apply_config()
        self.rescan_gifs()
        self.show_pet()
        self.play_next_gif()
        if self.config.weather_enabled:
            self.refresh_weather()

    def refresh_weather(self) -> None:
        self.weather_service.refresh(self.config)

    def weather_updated(self, info: WeatherInfo, success: bool) -> None:
        if not self.config.weather_enabled:
            return
        save_config(self.config)
        self.weather_temperature_text = info.weather_text
        if info.temperature is not None and info.weather_text != "未知":
            self.weather_temperature_text = f"{info.weather_text} {info.temperature:g}℃"
        elif info.weather_text == "未知":
            self.weather_temperature_text = "天气不可用"
        if info.wind_direction and info.wind_level:
            self.weather_wind_text = f"{info.wind_direction} {info.wind_level}"
        elif info.wind_direction and info.wind_speed is not None:
            self.weather_wind_text = f"{info.wind_direction} {info.wind_speed:g}km/h"
        else:
            self.weather_wind_text = info.wind_direction or (f"{info.wind_level}风" if info.wind_level else "")
        self.weather_page = 0
        self.update_weather_badge()
        self.weather_badge.show()
        if self.weather_wind_text:
            self.weather_rotate_timer.start()
        else:
            self.weather_rotate_timer.stop()

    def rotate_weather_badge(self) -> None:
        if not self.config.weather_enabled or not self.weather_wind_text:
            self.weather_rotate_timer.stop()
            return
        self.weather_page = 1 - self.weather_page
        self.update_weather_badge()

    def update_weather_badge(self) -> None:
        text = self.weather_wind_text if self.weather_page == 1 and self.weather_wind_text else self.weather_temperature_text
        self.weather_badge.setText(text)

    def reset_position(self) -> None:
        screen = QApplication.primaryScreen()
        if screen is not None:
            rect = screen.availableGeometry()
            self.move(rect.center().x() - self.width() // 2, rect.center().y() - self.height() // 2)
        else:
            self.move(120, 120)
        self.save_position()
        self.show_pet()

    def show_pet(self) -> None:
        self.show()
        self.raise_()
        self.activateWindow()

    def save_position(self) -> None:
        self.config.x = int(self.x())
        self.config.y = int(self.y())
        save_config(self.config)

    def exit_app(self) -> None:
        self.save_position()
        if self.tray is not None:
            self.tray.hide()
        QApplication.quit()

    def mousePressEvent(self, event) -> None:
        if event.button() == Qt.MouseButton.LeftButton:
            self.dragging = True
            self.drag_start = event.globalPosition().toPoint() - self.frameGeometry().topLeft()
            event.accept()
        elif event.button() == Qt.MouseButton.RightButton:
            self.show_context_menu(event.globalPosition().toPoint())

    def mouseMoveEvent(self, event) -> None:
        if self.dragging and event.buttons() & Qt.MouseButton.LeftButton:
            self.move(event.globalPosition().toPoint() - self.drag_start)
            event.accept()

    def mouseReleaseEvent(self, event) -> None:
        if event.button() == Qt.MouseButton.LeftButton:
            self.dragging = False
            self.save_position()
            event.accept()

    def mouseDoubleClickEvent(self, event) -> None:
        if event.button() == Qt.MouseButton.LeftButton:
            self.open_settings()

    def mousePressNext(self) -> None:
        self.play_next_gif()

    def show_context_menu(self, pos: QPoint) -> None:
        menu = QMenu(self)
        mood_menu = menu.addMenu("今日心情")
        for option in MOODS:
            action = QAction(option.name, mood_menu)
            action.triggered.connect(lambda checked=False, key=option.key: self.set_mood(key))
            mood_menu.addAction(action)
        menu.addSeparator()
        menu.addAction("下一张", self.play_next_gif)
        menu.addAction("设置", self.open_settings)
        menu.addAction("重新扫描 GIF", self.rescan_gifs)
        menu.addAction("重置位置", self.reset_position)
        menu.addSeparator()
        menu.addAction("退出", self.exit_app)
        menu.exec(pos)


def main() -> int:
    app = QApplication(sys.argv)
    app.setQuitOnLastWindowClosed(False)
    window = PetWindow()
    window.show_pet()
    return app.exec()


if __name__ == "__main__":
    raise SystemExit(main())
