"""Build the layered SVG illustration used by the scroll-driven experience."""
from pathlib import Path
import math
import random
import re

root = Path(__file__).resolve().parents[1]

def tree(x, y, scale=1, seed=1):
    rng = random.Random(seed)
    leaves = []
    palette = ['#a7aa68', '#c7b36c', '#bd9b55', '#d6bb75', '#8f9a63', '#ddd094']
    for row, (cy, count, spread) in enumerate([(-111, 6, 36), (-92, 9, 52), (-69, 9, 56), (-49, 7, 44)]):
        for i in range(count):
            cx = (i / max(1, count - 1) - .5) * spread * 2 + rng.uniform(-7, 7)
            yy = cy + rng.uniform(-9, 9)
            angle = rng.uniform(-45, 45)
            color = palette[rng.randrange(len(palette))]
            leaves.append(f'<path d="M-16 4Q-21-14 0-23Q24-12 17 9Q0 23-16 4Z" transform="translate({cx:.1f} {yy:.1f}) rotate({angle:.1f})" fill="{color}"/><path d="M{cx:.1f} {yy+10:.1f}l3-15" stroke="#8a8850" opacity=".18"/>')
    return f'''<g transform="translate({x} {y}) scale({scale})">
      <ellipse cy="7" rx="47" ry="12" fill="#8b9870" opacity=".16"/>
      <path d="M-7 3Q-6-46-14-82L-5-91Q1-64 3-42Q20-62 25-83L31-79Q27-53 6-28L9 4Z" fill="#8f7859"/>
      <path d="M-2-2L-3-40M-4-54L-24-77M5-29L20-46" fill="none" stroke="#b19b74" stroke-width="2"/>
      {''.join(leaves)}
      <g fill="#c6813f" stroke="#b97738" stroke-width=".8"><circle cx="-26" cy="-57" r="6"/><circle cx="23" cy="-65" r="6.5"/><circle cx="43" cy="-86" r="5.5"/><circle cx="-6" cy="-92" r="6"/></g>
      <g stroke="#69734c" stroke-width="2" fill="none"><path d="M-29-62l3 2 3-2M20-70l3 2 3-2M40-91l3 2 3-2M-9-97l3 2 3-2"/></g>
    </g>'''

roof_tiles = ''.join(f'<path d="M{x} 345Q{x+2} 352 {x-6} 361"/>' for x in range(665, 833, 8))
window_grid = ''.join(f'<path d="M{x} 377V411"/>' for x in range(710, 746, 7)) + ''.join(f'<path d="M705 {y}H746"/>' for y in range(381, 411, 7))
grass = ''.join(f'<path d="M{x} {y}q-5-8-9-10m9 10q0-10 5-14m-5 14q7-7 13-8"/>' for x,y in [(586,420),(620,449),(847,452),(900,434),(937,416),(660,422),(824,421)])
rain = ''.join(f'<path class="rain-streak" style="--drop-delay:-{i*.17:.2f}s" d="M{x} {y}l-8 22"/>' for i,(x,y) in enumerate([(602,224),(650,250),(697,219),(746,250),(797,220),(847,250),(898,226),(625,292),(674,324),(727,295),(778,327),(831,292),(877,326),(601,366),(701,369),(800,365),(909,365)]))

svg = f'''<svg viewBox="0 0 1000 540" aria-hidden="true" focusable="false">
<defs>
  <linearGradient id="cloudShade" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#fffef8"/><stop offset="1" stop-color="#e0e1d3"/></linearGradient>
  <linearGradient id="gardenShade" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#e4ddbc"/><stop offset="1" stop-color="#c9c6a3"/></linearGradient>
  <linearGradient id="sweaterShade" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#718c7b"/><stop offset="1" stop-color="#506f60"/></linearGradient>
  <linearGradient id="trouserShade" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#c8b893"/><stop offset="1" stop-color="#af9d79"/></linearGradient>
  <radialGradient id="atmosphere"><stop stop-color="#e7dfc5" stop-opacity=".62"/><stop offset="1" stop-color="#f0ebdf" stop-opacity="0"/></radialGradient>
  <filter id="softShadow" x="-30%" y="-50%" width="160%" height="200%"><feGaussianBlur stdDeviation="4"/></filter>
  <g id="cloudShape">
    <path d="M-84 29C-115 25-123-5-104-23C-97-31-88-34-77-33C-83-63-56-83-32-71C-20-95 19-88 28-62C52-79 82-60 81-34C108-32 122-6 109 15C95 40 38 45-7 43C-40 43-68 39-84 29Z" fill="url(#cloudShade)" stroke="#bdc4b2" stroke-width="1.4"/>
    <path d="M-101-12Q-91-30-76-25M-69-49Q-58-72-37-62M-20-73Q3-86 20-63M38-59Q67-66 73-40" fill="none" stroke="#fffef8" stroke-width="4" stroke-linecap="round" opacity=".85"/>
    <path d="M-81 28Q-12 47 73 28" fill="none" stroke="#c9cebd" stroke-width="2" opacity=".55"/>
  </g>
</defs>
<ellipse cx="556" cy="307" rx="438" ry="236" fill="url(#atmosphere)"/>
<g fill="none" stroke="#d9d0bd" stroke-width="1" opacity=".45"><path d="M25 453H975M235 453L162 527M543 453L588 527M923 453L980 506"/></g>
<!-- Woven rug, a ceramic planter, and a shaped swivel chair. -->
<ellipse cx="315" cy="468" rx="173" ry="47" fill="#b7a67e" opacity=".14" filter="url(#softShadow)"/>
<ellipse cx="315" cy="459" rx="169" ry="46" fill="#e2cfaa"/><ellipse cx="315" cy="457" rx="157" ry="39" fill="none" stroke="#eee2c6" stroke-width="3"/>
<ellipse cx="315" cy="457" rx="147" ry="34" fill="none" stroke="#cdb98e" stroke-width=".8" opacity=".55"/>
<g id="scene-planter" transform="translate(111 423)"><ellipse cy="9" rx="25" ry="6" fill="#cbbfaa" opacity=".3"/>
 <path d="M-19-37H21L15 5H-13Z" fill="#b99b7e"/><path d="M-21-40H23V-32H-21Z" fill="#cbb094"/><path d="M-12-29L-8 0" stroke="#d8c1a5" stroke-width="2"/>
 <path d="M1-40L1-148M1-84L-21-109M1-109L21-134M1-61L23-86" fill="none" stroke="#9aab8e" stroke-width="2.5"/>
 <path d="M0-106Q1-143 29-150Q37-121 0-106Z" fill="#a7b19a"/><path d="M0-80Q-32-79-36-116Q-7-122 0-80Z" fill="#b1b99e"/><path d="M1-57Q38-64 36-97Q8-92 1-57Z" fill="#9eae90"/>
 <path d="M4-113L23-140M-4-87L-28-108M7-65L28-89" stroke="#c2c9b1" fill="none" stroke-width="1"/>
</g>
<g id="chair"><ellipse cx="327" cy="459" rx="39" ry="9" fill="#768e7d" opacity=".2"/><ellipse cx="327" cy="453" rx="35" ry="8" fill="#a1b19c"/><path d="M327 389V451" stroke="#a9b6a3" stroke-width="10"/><path d="M330 391V446" stroke="#cbd1b9" stroke-width="3"/>
 <path d="M262 267Q241 268 244 296L251 365Q254 394 292 398H370Q392 396 395 381L393 374L281 365L279 281Q277 267 262 267Z" fill="#9eae99"/>
 <path d="M255 284L263 355Q264 379 292 382H377" fill="none" stroke="#c3cbb3" stroke-width="5" stroke-linecap="round"/>
 <path d="M255 323H291" stroke="#7d9380" stroke-width="7" stroke-linecap="round"/>
</g>
<!-- Small garden with a tiled-roof hanok, timber, lattice windows, and stone paths. -->
<g id="village">
 <ellipse cx="757" cy="452" rx="217" ry="42" fill="#c0b393" opacity=".2" filter="url(#softShadow)"/>
 <path d="M554 425Q576 388 634 391Q678 362 732 383Q795 362 846 391Q924 383 960 426Q943 466 848 477Q742 495 627 470Q573 459 554 425Z" fill="url(#gardenShade)"/>
 <path d="M555 427Q605 466 704 472Q862 492 957 430" fill="none" stroke="#b9b995" stroke-width="5" opacity=".65"/>
 <path d="M562 428Q636 458 697 446T883 439Q925 435 948 420" fill="none" stroke="#eee4c7" stroke-width="2"/>
 {tree(618, 413, .68, 4)}
 {tree(886, 431, 1.12, 12)}
 <ellipse cx="751" cy="428" rx="106" ry="20" fill="#9a9873" opacity=".16"/>
 <path d="M657 422L794 438L845 418L709 404Z" fill="#a9a38c"/><path d="M657 415L794 429L845 411V420L794 439L657 424Z" fill="#b6af96"/>
 <path d="M666 361L787 371V423L666 412Z" fill="#e6d8b8"/><path d="M787 371L833 355V408L787 423Z" fill="#d2c2a1"/>
 <path d="M670 367L785 377M670 404L785 414" stroke="#b7a17c" stroke-width="3"/>
 <g fill="#967957"><path d="M668 362L674 363V413L668 412Z"/><path d="M695 367L700 367V416L695 415Z"/><path d="M750 371L756 372V421L750 420Z"/><path d="M781 373L788 373V426L781 424Z"/></g>
 <path d="M706 375L746 379V413L706 409Z" fill="#8d7a5d"/><path d="M709 379L743 382V409L709 406Z" fill="#e7dcb9"/>
 <g stroke="#9f8b68" stroke-width="1" fill="none">{window_grid}</g>
 <path d="M764 383L779 384V419L764 417Z" fill="#a98e65"/><path d="M769 386V414M775 387V416" stroke="#c9b48a" stroke-width="1"/>
 <path d="M798 379L820 371V396L798 404Z" fill="#8c7a5c"/><path d="M801 382L817 377V394L801 399Z" fill="#daceab"/><path d="M809 379V396M801 390L817 385" stroke="#a08b65" stroke-width="1.5"/>
 <path d="M646 360Q671 354 695 329L799 337Q817 354 849 354L835 365L789 382L653 370Z" fill="#6e7a6c"/>
 <path d="M646 356Q674 350 697 324L798 333Q820 349 851 350Q828 363 789 374L650 365Z" fill="#87907a"/>
 <path d="M697 324L798 333L837 352L790 368L660 360Z" fill="#9b9f85"/>
 <g stroke="#73816f" stroke-width="2" opacity=".8" fill="none">{roof_tiles}</g>
 <path d="M650 364Q712 367 790 374L844 357" fill="none" stroke="#c0bda0" stroke-width="3" stroke-linecap="round"/>
 <path d="M693 323Q746 325 802 333" fill="none" stroke="#647462" stroke-width="5" stroke-linecap="round"/>
 <path d="M694 321Q745 323 801 331" fill="none" stroke="#b9b89c" stroke-width="2"/>
 <g fill="#e5ddc1" stroke="#beb89c" stroke-width=".8"><path d="M749 431l28 3-8 9-30-4Z"/><path d="M724 445l30 4-11 9-32-5Z"/><path d="M692 459l31 4-15 8-29-5Z"/></g>
 <g fill="#a89c7b"><ellipse cx="647" cy="437" rx="13" ry="5"/><ellipse cx="634" cy="430" rx="9" ry="5"/><ellipse cx="914" cy="453" rx="11" ry="4"/></g>
 <g stroke="#96996a" stroke-width="1.4" fill="none" stroke-linecap="round">{grass}</g>
 <g fill="#bd874a" opacity=".75"><ellipse cx="604" cy="443" rx="5" ry="2" transform="rotate(-25 604 443)"/><ellipse cx="850" cy="467" rx="5" ry="2"/><ellipse cx="911" cy="436" rx="4" ry="2" transform="rotate(30 911 436)"/></g>
</g>
<!-- The visitor is built in layers so hands, sleeves, and equipment remain articulated. -->
<g id="person">
 <path d="M291 351Q326 351 365 363Q384 366 386 386L392 438L374 444Q363 413 358 391L297 388Z" fill="#ac9d7d"/>
 <path d="M288 353Q307 357 341 371Q358 377 359 397L351 445L332 444L336 396Q305 389 283 374Z" fill="url(#trouserShade)"/>
 <path d="M301 365Q319 377 342 381M346 397L340 432M376 395L382 429" fill="none" stroke="#ddcba6" stroke-width="2" opacity=".6"/>
 <path d="M332 441L352 441L354 449Q379 453 379 460Q361 467 329 460Z" fill="#f8f3df" stroke="#b3ad94" stroke-width="1.2"/>
 <path d="M374 438L392 435L399 442Q420 445 420 452Q402 460 371 452Z" fill="#f7f2df" stroke="#b3ad94" stroke-width="1.2"/>
 <path d="M332 456Q352 461 375 458M376 450Q400 456 417 451M345 447l9 4m-3-5 9 4M389 442l9 3m-4-5 9 3" fill="none" stroke="#cbc4ac" stroke-width="1.2"/>
 <path d="M290 249Q270 254 268 278L272 326L269 359Q300 375 337 358L334 279Q333 256 315 250Z" fill="url(#sweaterShade)"/>
 <path d="M278 274Q275 313 281 350M326 280L329 347M286 353Q307 361 328 353" fill="none" stroke="#8ea38c" stroke-width="1.6" opacity=".7"/>
 <path d="M272 356Q305 370 336 356L336 365Q307 378 271 365Z" fill="#506c5d"/>
 <path d="M293 226L292 250Q301 262 316 252L313 225Z" fill="#d3ac87"/>
 <path d="M294 240Q303 246 313 239" fill="none" stroke="#bd9272" stroke-width="1.2"/>
 <path d="M280 219Q267 207 273 183Q277 162 299 158Q325 154 338 177Q344 194 337 219L327 233L283 230Z" fill="#3d5148"/>
 <path d="M286 188Q300 188 315 173Q319 188 330 192L332 213Q336 219 329 222Q324 238 311 242Q296 241 288 227L281 208Z" fill="#e0bc94"/>
 <path d="M281 209Q271 199 274 189Q270 167 296 158Q278 180 286 194L287 220Q282 231 273 227Z" fill="#40564b"/>
 <path d="M303 161Q324 157 334 175L338 194Q329 185 322 181Q313 185 303 183Q311 173 303 161Z" fill="#354d42"/>
 <path d="M278 188Q278 173 291 168M327 177Q332 183 332 188" fill="none" stroke="#607367" stroke-width="1.5" opacity=".65"/>
 <ellipse cx="287" cy="211" rx="5" ry="7" fill="#d7ae88"/><path d="M284 210q4-3 5 3" stroke="#bd9374" fill="none" stroke-width="1"/>
 <path d="M315 201q5-3 9 0" fill="none" stroke="#665d4a" stroke-width="1.3" stroke-linecap="round"/><ellipse cx="320" cy="207" rx="1.6" ry="2.1" fill="#454c3e"/>
 <path d="M325 211l2 7-4 1M314 228q6 4 11-1" fill="none" stroke="#aa7c5f" stroke-width="1.2" stroke-linecap="round"/><ellipse cx="313" cy="219" rx="5" ry="2.8" fill="#d69d7d" opacity=".4"/>
 <path d="M290 248Q302 258 316 250" fill="none" stroke="#3f5f50" stroke-width="4"/>
 <g id="headset"><path d="M278 200L313 202" stroke="#e9e6d9" stroke-width="10"/><path d="M281 197L310 198" stroke="#faf8ee" stroke-width="2"/>
  <path d="M312 186Q329 182 349 187L353 209Q335 217 312 210Z" fill="#eeeede" stroke="#a4b0a1" stroke-width="1.5"/><path d="M316 189Q334 186 347 190L348 205Q333 210 316 206Z" fill="#fafaf1"/>
  <g fill="#7a887b"><ellipse cx="324" cy="197" rx="1.8" ry="3"/><ellipse cx="335" cy="197" rx="1.8" ry="3"/><ellipse cx="345" cy="196" rx="1.5" ry="2.5"/></g><path d="M320 212L342 212" stroke="#809081" stroke-width="2"/>
 </g>
 <path id="arm-back" fill="none" stroke="#4e705f" stroke-width="17" stroke-linecap="round"/>
 <path id="arm-back-seam" fill="none" stroke="#7e9982" stroke-width="1.3" opacity=".55"/>
 <path id="arm-front" fill="none" stroke="#668673" stroke-width="20" stroke-linecap="round"/>
 <path id="arm-front-seam" fill="none" stroke="#94aa90" stroke-width="1.4" opacity=".6"/>
 <g id="hand-back"><path d="M-7-7Q-1-12 6-7L11-1Q12 7 5 9L-5 7Z" fill="#d8b28a"/><g id="controller-back"><path d="M-4-17Q2-20 7-15L6 5Q1 10-4 5Z" fill="#f8f6e9" stroke="#a1ad9b" stroke-width="1"/><ellipse cy="-15" rx="7" ry="4" fill="#d9dfd0"/><ellipse cy="-16" rx="3" ry="1.8" fill="#81917e"/><path d="M6-2Q12-3 9 3" fill="none" stroke="#d8b28a" stroke-width="4" stroke-linecap="round"/></g></g>
 <g id="hand-front"><path d="M-8-8Q-2-13 6-8L12-1Q13 8 5 10L-6 8Z" fill="#e0bc95"/><g id="controller-front"><path d="M-5-18Q1-22 7-17L6 5Q1 11-5 6Z" fill="#fffdf0" stroke="#a1ad9b" stroke-width="1.2"/><ellipse cy="-17" rx="8" ry="4.5" fill="#e5e8db"/><ellipse cy="-18" rx="3" ry="2" fill="#81917e"/><path d="M6-1Q13-2 9 5" fill="none" stroke="#e0bc95" stroke-width="4.5" stroke-linecap="round"/></g></g>
</g>
<g id="controller-rays" fill="none" stroke="#9cae94" stroke-width="1.3" stroke-dasharray="3 7"><path id="ray-left"/><path id="ray-right"/></g>
<g id="rain" fill="none" stroke="#829d97" stroke-width="1.8" stroke-linecap="round">{rain}</g>
<g id="cloud-left"><use href="#cloudShape"/></g><g id="cloud-right"><use href="#cloudShape"/></g>
<g id="cloud-gauge" transform="translate(720 152)">
 <circle r="93" fill="none" stroke="#b5bca5" stroke-width="3" opacity=".35"/>
 <circle id="gauge-fill" r="93" fill="none" stroke="#b38a45" stroke-width="4.5" stroke-linecap="round" pathLength="100" stroke-dasharray="100" stroke-dashoffset="100" transform="rotate(-90)"/>
 <circle r="101" fill="none" stroke="#c7bf9f" stroke-width=".6" opacity=".6"/>
 <g transform="translate(0 115)"><rect x="-30" y="-13" width="60" height="26" rx="13" fill="#f6f0e3"/><text id="gauge-value" text-anchor="middle" dominant-baseline="central" fill="#97703b" font-family="Georgia,serif" font-size="15">0%</text></g>
</g>
<g id="rain-rings" fill="none" stroke="#9baea1" stroke-width="1.3"><ellipse cx="626" cy="433" rx="17" ry="4"/><ellipse cx="818" cy="460" rx="21" ry="5"/><ellipse cx="899" cy="444" rx="13" ry="3"/><ellipse cx="691" cy="475" rx="11" ry="3"/></g>
</svg>'''
page = root / 'output' / 'experience.html'
html = page.read_text(encoding='utf-8')
html, count = re.subn(r'(<div class="journey-scene">)<svg.*?</svg>(</div>)', lambda m: m[1] + svg + m[2], html, count=1, flags=re.S)
assert count == 1
page.write_text(html, encoding='utf-8')
print('Updated the experience illustration.')
