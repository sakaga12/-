// html/*.html 을 652x488 (2배 해상도) PNG로 찍는다. 크몽 권장 비율 4:3.
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

(async () => {
  const dir = path.join(__dirname, 'html');
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 652, height: 488 }, deviceScaleFactor: 2 });
  for (const f of fs.readdirSync(dir).filter((f) => f.endsWith('.html'))) {
    await page.goto('file://' + path.join(dir, f));
    await page.evaluate(() => document.fonts.ready);
    const out = path.join(__dirname, f.replace('.html', '.png'));
    await page.screenshot({ path: out });
    console.log(out);
  }
  await browser.close();
})();
