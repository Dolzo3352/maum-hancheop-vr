"""Check the single-page journey, photo separation, and local link targets."""
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import urlsplit, unquote

root = Path(__file__).resolve().parents[1] / 'output'

class Page(HTMLParser):
    def __init__(self):
        super().__init__()
        self.ids, self.links, self.assets, self.gallery = [], [], [], []

    def handle_starttag(self, tag, attrs):
        keys = [key for key, _ in attrs]
        assert len(keys) == len(set(keys)), f'Duplicate attributes: {tag}'
        a = dict(attrs)
        if 'id' in a:
            assert a['id'] not in self.ids, f'Duplicate ID: {a["id"]}'
            self.ids.append(a['id'])
        if tag == 'a':
            self.links.append(a.get('href', ''))
        if tag in ('img', 'script', 'link'):
            self.assets.append(a.get('src', a.get('href', '')))
        if 'data-gallery' in a:
            self.gallery.append(a['data-full'])

pages = {}
for path in root.glob('*.html'):
    page = Page()
    page.feed(path.read_text(encoding='utf-8'))
    pages[path.name] = page

for name, page in pages.items():
    for link in page.links + page.assets + page.gallery:
        url = urlsplit(link)
        if url.scheme or url.netloc:
            continue
        target = unquote(url.path) or name
        assert (root / target).exists(), (name, link)
        if url.fragment and target in pages:
            assert url.fragment in pages[target].ids, (name, link)

home = pages['index.html']
order = ['film', 'story', 'world', 'experience', 'journey', 'visit', 'credits']
assert [home.ids.index(key) for key in order] == sorted(home.ids.index(key) for key in order)
assert '#experience' in home.links
assert 'experience.html' not in home.links
assert 'playtest.html' in home.links
assert not any('images/playtest/' in asset for asset in home.assets)
assert len(home.gallery) == 5
assert len(pages['playtest.html'].gallery) == 2
assert all('images/playtest/' in asset for asset in pages['playtest.html'].gallery)
assert 'visitor-face' in home.ids and 'gauge-fill' in home.ids
assert 'journey' not in pages['experience.html'].ids
print('Passed: page order, embedded journey, separate playtest photos, galleries, local links and unique IDs.')
