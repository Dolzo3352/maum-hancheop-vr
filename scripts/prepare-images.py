"""Build responsive image assets without changing the source screenshots."""
from pathlib import Path
from PIL import Image, ImageOps

root = Path(__file__).resolve().parents[1] / 'output'
source = root / 'images' / 'world'
destination = root / 'images' / 'optimized'
destination.mkdir(parents=True, exist_ok=True)
for path in sorted(source.glob('*.png')):
    with Image.open(path) as image:
        image = image.convert('RGB')
        for target in (640, 1280, 1600):
            width = min(target, image.width)
            height = round(image.height * width / image.width)
            image.resize((width, height), Image.Resampling.LANCZOS).save(
                destination / f'{path.stem}-{target}.webp', quality=84, method=6)
with Image.open(source / '03-world-full.png') as image:
    ImageOps.fit(image.convert('RGB'), (1200, 630), method=Image.Resampling.LANCZOS).save(
        root / 'images' / 'og-world.jpg', quality=88, optimize=True)
original = sum(path.stat().st_size for path in source.glob('*.png'))
optimized = sum(path.stat().st_size for path in destination.glob('*-1280.webp'))
print(f'Original gallery: {original:,} bytes; 1280px gallery: {optimized:,} bytes; reduction: {100 * (1 - optimized / original):.1f}%')
