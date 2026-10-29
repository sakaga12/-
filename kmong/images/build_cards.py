"""크몽 대표 이미지·포트폴리오 이미지용 HTML을 만든다. render.js 가 PNG로 찍는다.

포트폴리오 이미지에 들어가는 숫자는 portfolio/ 샘플의 실제 실행 결과에서 읽어온다.
"""
from html import escape
from pathlib import Path

import pandas as pd

ROOT = Path(__file__).parent
PF = ROOT.parent / "portfolio"
OUT = ROOT / "html"

CSS = """
@import url('https://fonts.googleapis.com/css2?family=Noto+Sans+KR:wght@400;500;700;900&display=block');
*{margin:0;padding:0;box-sizing:border-box}
body{width:652px;height:488px;font-family:'Noto Sans KR',sans-serif;overflow:hidden;
  word-break:keep-all;color:#0f172a;background:#fff}
.thumb{height:100%;padding:44px 44px 36px;display:flex;flex-direction:column;color:#fff}
.thumb .tag{display:inline-block;font-size:15px;font-weight:700;padding:6px 14px;border-radius:999px;
  background:rgba(255,255,255,.16);align-self:flex-start}
.thumb h1{font-size:46px;line-height:1.22;font-weight:900;margin-top:22px;letter-spacing:-1px}
.thumb h1 em{font-style:normal;color:var(--accent)}
.thumb p{font-size:19px;margin-top:14px;opacity:.85;line-height:1.5}
.thumb .chips{margin-top:auto;display:flex;gap:10px;flex-wrap:wrap}
.thumb .chips span{font-size:15px;font-weight:700;padding:8px 14px;border-radius:10px;
  background:#fff;color:var(--bg)}
.pf{height:100%;padding:26px 28px;display:flex;flex-direction:column;background:#f1f5f9}
.pf .head{display:flex;align-items:baseline;gap:10px}
.pf .no{font-size:13px;font-weight:700;color:#fff;background:#1e3a8a;padding:3px 10px;border-radius:6px}
.pf h2{font-size:23px;font-weight:900;letter-spacing:-.5px}
.pf .sub{font-size:14px;color:#475569;margin-top:6px}
.cols{display:flex;gap:12px;margin-top:16px;flex:1;min-height:0}
.box{flex:1;background:#fff;border-radius:12px;padding:14px;display:flex;flex-direction:column;
  border:1px solid #e2e8f0;min-width:0;overflow:hidden}
.box.after{flex:1.3;border:2px solid #2563eb}
.box .lbl{font-size:13px;font-weight:900;color:#64748b;margin-bottom:8px}
.box.after .lbl{color:#2563eb}
.arrow{align-self:center;font-size:26px;color:#2563eb;font-weight:900}
table{border-collapse:collapse;width:100%;font-size:11px;table-layout:fixed}
th,td{padding:4px 5px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
th{background:#1e3a8a;color:#fff;font-weight:700;text-align:left}
td{border-bottom:1px solid #e2e8f0}
.unit{font-size:10px;color:#64748b;text-align:right;margin-bottom:3px}
td.n{text-align:right;font-variant-numeric:tabular-nums}
.bad{color:#dc2626;font-weight:700}
.file{flex-shrink:0;font-size:11px;padding:2px 8px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:6px;
  margin-bottom:3px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.file:before{content:'📄 '}
.stats{display:flex;gap:10px;margin-top:14px}
.stat{flex:1;background:#1e3a8a;color:#fff;border-radius:10px;padding:10px 12px}
.stat b{display:block;font-size:22px;font-weight:900}
.stat span{font-size:12px;opacity:.85}
"""


def page(body: str) -> str:
    return f"<!doctype html><html><head><meta charset='utf-8'><style>{CSS}</style></head><body>{body}</body></html>"


def thumb(bg, accent, tag, title, desc, chips):
    chip_html = "".join(f"<span>{escape(c)}</span>" for c in chips)
    return page(f"""<div class="thumb" style="background:{bg};--bg:{bg};--accent:{accent}">
      <div class="tag">{escape(tag)}</div><h1>{title}</h1><p>{desc}</p>
      <div class="chips">{chip_html}</div></div>""")


def table(df: pd.DataFrame, num_cols=(), bad=lambda col, v: False, widths=None) -> str:
    cols = "".join(f"<col style='width:{w}%'>" for w in widths) if widths else ""
    head = "".join(f"<th>{escape(str(c))}</th>" for c in df.columns)
    rows = ""
    for _, r in df.iterrows():
        cells = ""
        for c in df.columns:
            v = r[c]
            cls = ["n"] if c in num_cols else []
            if bad(c, v):
                cls.append("bad")
            text = "(비어있음)" if pd.isna(v) else (f"{v:,.0f}" if c in num_cols else str(v))
            cells += f"<td class='{' '.join(cls)}'>{escape(text)}</td>"
        rows += f"<tr>{cells}</tr>"
    return f"<table>{cols}<tr>{head}</tr>{rows}</table>"


def portfolio(no, title, sub, before, after, stats):
    stat_html = "".join(f"<div class='stat'><b>{escape(v)}</b><span>{escape(k)}</span></div>"
                        for k, v in stats)
    return page(f"""<div class="pf"><div class="head"><span class="no">포트폴리오 {no}</span>
      <h2>{escape(title)}</h2></div><div class="sub">{escape(sub)}</div>
      <div class="cols"><div class="box"><div class="lbl">BEFORE</div>{before}</div>
      <div class="arrow">→</div><div class="box after"><div class="lbl">AFTER</div>{after}</div></div>
      <div class="stats">{stat_html}</div></div>""")


def sales_card():
    d = PF / "01-sales-report"
    files = sorted(p.name for p in (d / "input").glob("*.xlsx"))
    report = d / "output" / "매출_통합보고서.xlsx"
    monthly = pd.read_excel(report, sheet_name="월별매출")
    products = pd.read_excel(report, sheet_name="상품별")
    raw = pd.read_excel(report, sheet_name="전체내역")
    before = "".join(f"<div class='file'>{escape(f)}</div>" for f in files)
    money = [c for c in monthly.columns if c != "월"]
    monthly[money] = monthly[money] / 10000
    monthly["월"] = monthly["월"].map(lambda m: f"{int(m[5:])}월")
    products["매출"] = products["매출"] / 10000
    after = ("<div class='unit'>단위: 만원</div>"
             + table(monthly, num_cols=set(money), widths=[11, 21, 25, 20, 23])
             + "<div style='height:10px'></div>"
             + table(products.head(3)[["상품명", "판매수량", "매출"]], num_cols={"판매수량", "매출"}))
    return portfolio(1, "지점별 매출 엑셀 통합 보고서 자동화",
                     "흩어진 월별·지점별 파일을 실행 한 번으로 합쳐 요약표와 차트까지 만듭니다",
                     before, after,
                     [("합친 파일", f"{len(files)}개"), ("통합한 거래", f"{len(raw):,}건"),
                      ("작업 시간", "클릭 1번")])


def shipping_card():
    d = PF / "03-shipping-label"
    orders = pd.read_excel(d / "input" / "주문내역.xlsx")
    label_file = next((d / "output").glob("송장등록_*.xlsx"))
    labels = pd.read_excel(label_file, sheet_name="송장등록")
    problems = pd.read_excel(label_file, sheet_name=1)
    raw_phone = lambda c, v: c == "수취인연락처" and not str(v).startswith("010-")
    shown = orders[orders["주문상태"] != "결제완료"].head(1)
    shown = pd.concat([orders.head(7), shown]).sort_values("주문번호")
    shown = shown.rename(columns={"수취인연락처": "연락처", "주문상태": "상태"})
    raw_phone = lambda c, v: c == "연락처" and not str(v).startswith("010-")
    before = table(shown[["수취인", "연락처", "상태"]], widths=[20, 52, 28],
                   bad=lambda c, v: raw_phone(c, v) or (c == "상태" and v != "결제완료"))
    labels = labels.rename(columns={"받는분 전화번호": "전화번호", "총수량": "수량"})
    after = table(labels.head(8)[["받는분", "전화번호", "품목", "수량"]],
                  widths=[16, 33, 39, 12])
    return portfolio(2, "주문 엑셀 → 택배 송장 양식 자동 변환",
                     "같은 사람 주문은 한 상자로 합치고, 전화번호를 통일하고, 취소·주소 누락을 걸러냅니다",
                     before, after,
                     [("주문", f"{len(orders)}건"), ("송장", f"{len(labels)}장"),
                      ("자동으로 걸러낸 오류", f"{len(problems)}건")])


def scraper_card():
    books = pd.read_excel(PF / "02-web-scraper" / "output" / "도서목록.xlsx")
    before = ("<div style='font-size:13px;line-height:1.7;color:#334155'>"
              "웹사이트 50페이지를<br>한 장씩 넘기며<br>제목·가격·평점을<br>"
              "<b class='bad'>손으로 복사</b>하던 작업<br><br>"
              "<div class='file'>books.toscrape.com</div>"
              "<span style='font-size:11px;color:#64748b'>※ 크롤링 연습용으로 공개된 사이트</span></div>")
    view = books.head(8)[["제목", "가격(GBP)", "평점"]].rename(columns={"가격(GBP)": "가격"})
    view["가격"] = view["가격"].map(lambda v: f"£{v:.2f}")
    view["평점"] = view["평점"].map(lambda v: "★" * v)
    after = table(view, widths=[56, 18, 26])
    return portfolio(3, "웹사이트 상품 목록 수집 → 엑셀 정리",
                     "50페이지에 흩어진 상품 정보를 모아 정렬·필터·링크가 걸린 엑셀로 납품합니다",
                     before, after,
                     [("수집 페이지", "50쪽"), ("수집 상품", f"{len(books):,}개"),
                      ("소요 시간", "약 1분")])


def main():
    OUT.mkdir(exist_ok=True)
    cards = {
        "thumb-1-automation": thumb(
            "#1e3a8a", "#fde047", "엑셀 · 구글시트 업무 자동화",
            "매일 1시간 엑셀 작업,<br><em>버튼 한 번</em>으로",
            "파일 합치기 · 보고서 집계 · 송장 정리<br>반복 업무를 자동으로 바꿔 드립니다",
            ["당일 상담", "빠른 납품", "7일 무료 오류수정"]),
        "thumb-2-crawling": thumb(
            "#14532d", "#86efac", "웹 데이터 수집 · 크롤링",
            "일일이 복사하지 마세요<br><em>엑셀로 한 번에</em>",
            "상품 · 가격 · 목록 데이터를<br>정리된 엑셀 파일로 받아보세요",
            ["공개 데이터만 수집", "프로그램 납품 가능", "7일 무료 오류수정"]),
        "portfolio-1-sales-report": sales_card(),
        "portfolio-2-shipping-label": shipping_card(),
        "portfolio-3-web-scraper": scraper_card(),
    }
    for name, html in cards.items():
        (OUT / f"{name}.html").write_text(html, encoding="utf-8")
    print("\n".join(cards))


if __name__ == "__main__":
    main()
