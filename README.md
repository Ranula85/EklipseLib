# EklipseLib
### A custom C# framework based on Raylib-cs and expanding on it to include proper handling of assets, audio and UI interfaces.

This repository includes the source files for a custom extension of Raylib-cs that gives some extra tools for easier gamemaking.
- The `Sprite`,`TextureRegion` and `TextureAtlas` class, for handling textures and sprites drawn on screen
- The `UIPage` class that encapsulates all of the different `UIElement`s (for now, these are `Button`s, `Label`s and `Panel`s)
- The `AudioManager` that handles the `MUSIC` and `SFX` clases and serves as a static way to play sounds.
- The `Scene` class, which for now only wraps the `UIPage` and some helpful functions for it into a class.

### Additional notes:
- This framework is meant to have a specific file system for it. More information about it can be found in [this repository's wiki page](https://github.com/Ranula85/EklipseLib/wiki)
- The UI features self-scaling (keeping the aspect ratio) if the screen is not big enough to fit it.
- The AudioManager features looping and starting at specific moments for `MUSIC` types.

## FUTURE PLANS:
The reason for this framework to exist is becuase I wanted some foundation from which to build my own game. Because of that, it is focused mainly of UI, building it and using it efficiently. However, in the future, I plan to include the following features:
- Transparency for UI elements
- Allow changing UI Page bg color from the helper tool
- Improve looping capabilities to create looping sections that can be broken to make the song continue.
- `Animation` and `AnimatedSprite` classes in order to easily handle animations for sprites.
- A world space (thought to be in 2D for now) in which to add objects that get drawn on screen, this would also mean there would be a movable camera and probably integrated physics
