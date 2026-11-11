"""Compatibility entry point for the hard-pixel stages 1-3 frame removal.

Default is preview only; --apply explicitly updates only background_game.png.
See RemoveStageFrameCrisp.py for the reviewed original-source/nearest-pixel path.
The previous Telea inpaint and edge feather are intentionally no longer used.
"""
from RemoveStageFrameCrisp import main

if __name__ == '__main__':
    main()
