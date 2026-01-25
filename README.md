# SnowblindMod-Player

A modern, modular music player with support for various audio formats and customizable playback features.

## Features

- 🎵 Support for multiple audio formats (MP3, WAV, FLAC, OGG, etc.)
- 🎨 Modular architecture with plugin support
- 🎛️ Customizable playback controls
- 📝 Playlist management
- 🔊 Audio effects and equalizer support
- 🎯 Cross-platform compatibility

## Installation

```bash
# Clone the repository
git clone https://github.com/Snow-IT/SnowblindMod-Player.git
cd SnowblindMod-Player

# Install dependencies
pip install -r requirements.txt

# Run the player
python -m snowblind_player
```

## Quick Start

```python
from snowblind_player import Player

# Initialize the player
player = Player()

# Load and play a track
player.load("path/to/audio/file.mp3")
player.play()
```

## Project Structure

```
SnowblindMod-Player/
├── src/
│   └── snowblind_player/      # Main package
│       ├── core/              # Core player functionality
│       ├── plugins/           # Plugin system
│       ├── ui/                # User interface components
│       └── utils/             # Utility functions
├── tests/                     # Test suite
├── docs/                      # Documentation
├── examples/                  # Example usage scripts
└── requirements.txt           # Python dependencies
```

## Development

### Setting up Development Environment

```bash
# Create virtual environment
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate

# Install development dependencies
pip install -r requirements-dev.txt

# Run tests
pytest
```

### Running Tests

```bash
# Run all tests
pytest

# Run with coverage
pytest --cov=snowblind_player

# Run specific test file
pytest tests/test_player.py
```

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details on how to contribute to this project.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Roadmap

- [x] Basic project structure
- [ ] Core player implementation
- [ ] Plugin system
- [ ] UI components
- [ ] Audio effects processing
- [ ] Playlist management
- [ ] Documentation
- [ ] Release v1.0.0

## Support

For questions, issues, or feature requests, please open an issue on [GitHub Issues](https://github.com/Snow-IT/SnowblindMod-Player/issues).

## Acknowledgments

- Built with Python and modern audio libraries
- Inspired by modular audio player architectures