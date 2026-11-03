"""Local PNG contract checks; run: python -m unittest discover -s Tools/tests."""
from pathlib import Path
import unittest
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'Assets/Holedown/Art/Theme'


class ThemeAssetTests(unittest.TestCase):
    def test_m1_subjects_have_real_alpha_and_clear_corners(self):
        for name in ('ball.png', 'block_square.png', 'launcher.png'):
            with self.subTest(name=name), Image.open(ART / name) as image:
                self.assertEqual(image.mode, 'RGBA')
                alpha = np.array(image.getchannel('A'))
                self.assertEqual(int(alpha.min()), 0)
                self.assertEqual(int(alpha.max()), 255)
                self.assertTrue(np.all(alpha[0] == 0))
                self.assertTrue(np.all(alpha[-1] == 0))
                self.assertTrue(np.all(alpha[:, 0] == 0))
                self.assertTrue(np.all(alpha[:, -1] == 0))
                self.assertGreater(np.count_nonzero(alpha), alpha.size * .15)

    def test_launcher_has_clear_hole(self):
        with Image.open(ART / 'launcher.png') as image:
            self.assertEqual(image.getpixel((image.width//2,image.height//2))[3], 0)

    def test_background_retains_original_canvas_and_full_frame(self):
        with Image.open(ART / 'background_game.png') as image:
            self.assertEqual(image.size, (941,1672))
            self.assertEqual(image.mode, 'RGB')
            a = np.array(image)
            self.assertGreater(int(a[25,470].mean()), int(a[400,470].mean()))
            # No gold ring remains in the repaired mounting-plate region.
            patch=a[43:126,428:514].astype(int)
            self.assertFalse(np.any((patch[:,:,0]-patch[:,:,2]>35)&(patch[:,:,0]>100)))


if __name__ == '__main__':
    unittest.main()
