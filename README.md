> This is not an official Tilt Five product. Read [the disclaimer](#disclaimer) for details.
# GridGames for Tilt Five
![Preview Screenshot!](/Recordings/Screenshot.jpg)
</br><sub>Screenshots - Video Previews here: [Checkers](https://github.com/KasHunt/T5Doodle_GridGames/raw/master/Recordings/previewCheckers.mp4) | [Chess](https://github.com/KasHunt/T5Doodle_GridGames/raw/master/Recordings/previewChess.mp4) | [Reversi](https://github.com/KasHunt/T5Doodle_GridGames/raw/master/Recordings/previewReversi.mp4)</sub>

## Description
A small collection of grid based games for Tilt Five.

You'll need Tilt Five Glasses to run this application. 
If you don't know what this is, visit the [Tilt Five website](https://tiltfive.com)
to discover the future of tabletop AR.  

The second in (hopefully) a number of programming 'doodles' (short, simple, unrefined projects).
This particular one took me ~20 hours. Re-familiarizing myself with a number of Unity concepts 
and refining some of the Tilt Five specific utilities and wiedgets.

This collection includes a number of grid based games:
- Chess
- Reversi
- Checkers
 
PRs/feedback/bugs welcome, though no guarantees that this project will get any
love after it's posted.

## Usage
### Menu
The cog icon brings up the settings menu:
- New Game - Reset the current game (Won't reset games that aren't currently visible)
- Change Game - Switch to one of the other games
- Options - Show controls for sound volume and wand arc length
- Quit - Exit the application

The question mark icon brings up the help screen.

### Wand controls
| Control | Action                                                                           |
|---------|----------------------------------------------------------------------------------|
| Trigger | *(While playing)* Click and hold to move pieces<br/>*(In Menus)* Click to select |

## Future Ideas / Known Issues
- [Issue] Chess determines game state by replaying a move queue, but it's currently uncached which may be slow - add caching of the current state.
- [Issue] Overlay UI is still clickable (but invisible)
- [Enhancement] Desktop overhead spectator mode
- [Enhancement] Add more grid based games

## Development Time
- For v0.1.0 Approximately 20 hours 
  - Chess took the longest (complex rules, modelling chess pieces) ~9hrs
  - Reversi and Checkers roughly ~4hrs each
  - Grid board framework (several iterations) ~3hrs 

## Tooling
- Unity 2021.3 **: Game Engine**
- JetBrains Rider 2023.2 **: IDE**
- Blender 3.4 **: 3D Model Creation**
- Adobe Illustrator 2023 **: Vector Image Editing**
- Adobe Photoshop 2023 **: Bitmap Image Editing**
- Adobe Audition 2023 **: Audio Editing**
- Adobe Premier Pro 2023 **: Video Editing**

## Disclaimer
This application was personally developed by Kasper John Hunt, who has a
professional association with Tilt Five, the producer of Tilt Five augmented
reality headset on which the application can be run. However, please be advised
that this application is a personal and independent project.

It is not owned, approved, endorsed, or otherwise affiliated with
Tilt Five in any official capacity.

The views, ideas, and content expressed within this application are solely those
of the creator and do not reflect the opinions, policies, or positions of Tilt Five.
Any use of the Tilt Five's name, trademarks, or references to its products is for
descriptive purposes only and does not imply any association or sponsorship by Tilt Five.

Users of this application should be aware that it is provided "as is" without any
warranties or representations of any kind. Any questions, comments, or concerns
related to this application should be directed to Kasper Hunt and not to Tilt Five.

## Copyright
Copyright 2023 Kasper John Hunt

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

## Trademarks and Logos 
The Beachview Studios name and its associated logos are trademarks
of Beachview Studios, and may not be used without explicit written
permission from the trademark owner.

Unless you have explicit, written permission, you may not:

- Reproduce or use the images, logos, or trademarks of Beachview
  Studios in relation to any project that is not directly associated
  with or approved by Beachview Studios.
- Use any name, logo, or trademark of Beachview Studios to endorse
  or promote products or services derived from this software.
