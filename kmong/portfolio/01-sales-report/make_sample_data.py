"""포트폴리오용 가짜 매출 데이터 생성. 지점 3곳 x 3개월 = 엑셀 9개."""
import random
from datetime import date, timedelta
from pathlib import Path

from openpyxl import Workbook

random.seed(7)

BRANCHES = ["강남점", "홍대점", "부산서면점"]
PRODUCTS = {
    "아메리카노": 4500, "카페라떼": 5000, "바닐라라떼": 5500, "콜드브루": 5000,
    "자몽에이드": 6000, "치즈케이크": 6500, "크로플": 5500, "샌드위치": 7000,
}
MONTHS = [(2026, 7), (2026, 8), (2026, 9)]

out = Path(__file__).parent / "input"
out.mkdir(exist_ok=True)

for branch in BRANCHES:
    for year, month in MONTHS:
        wb = Workbook()
        ws = wb.active
        ws.title = "판매내역"
        ws.append(["일자", "상품명", "수량", "단가", "금액"])
        d = date(year, month, 1)
        while d.month == month:
            for _ in range(random.randint(6, 12)):
                name = random.choice(list(PRODUCTS))
                qty = random.randint(3, 40)
                price = PRODUCTS[name]
                ws.append([d, name, qty, price, qty * price])
            d += timedelta(days=1)
        wb.save(out / f"{branch}_{year}-{month:02d}_매출.xlsx")

print(f"{len(BRANCHES) * len(MONTHS)}개 파일 생성: {out}")
