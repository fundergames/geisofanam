"""
Entry point for Discord coordination bot.
Run: python -m discord_agent_bot.main
"""

# Load .env before config is imported so env vars are available
from pathlib import Path
from dotenv import load_dotenv
load_dotenv(Path(__file__).resolve().parent / ".env")

import logging
from .bot import CoordinationBot
from .config import get_config
from .file_watcher import start_file_watcher

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)
logger = logging.getLogger(__name__)


def main() -> None:
    """Start bot and optional file watcher."""
    env_path = Path(__file__).resolve().parent / ".env"
    load_dotenv(env_path)

    cfg = get_config()
    if not cfg.token:
        logger.error("DISCORD_BOT_TOKEN not set. Copy .env.example to .env and configure.")
        return

    project_root = Path(__file__).resolve().parent.parent.parent
    bot = CoordinationBot(project_root=project_root)

    watcher = None
    if cfg.watch_features:
        watcher = start_file_watcher(project_root, bot)
        watcher.start()
        logger.info("File watcher started for %s", cfg.features_path)

    try:
        bot.run(cfg.token, log_handler=None)
    finally:
        if watcher:
            watcher.stop()


if __name__ == "__main__":
    main()
