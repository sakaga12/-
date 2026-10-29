"""input 폴더의 지점별·월별 매출 엑셀을 모두 합쳐 요약 보고서를 만든다.

파일명 규칙: {지점}_{YYYY-MM}_매출.xlsx
실행: python merge_report.py  ->  output/매출_통합보고서.xlsx
"""
from pathlib import Path

import pandas as pd
from openpyxl import load_workbook
from openpyxl.chart import BarChart, LineChart, Reference
from openpyxl.styles import Alignment, Border, Font, PatternFill, Side
from openpyxl.utils import get_column_letter

BASE = Path(__file__).parent
INPUT, OUTPUT = BASE / "input", BASE / "output"
REPORT = OUTPUT / "매출_통합보고서.xlsx"

HEADER_FILL = PatternFill("solid", fgColor="1F4E78")
HEADER_FONT = Font(bold=True, color="FFFFFF")
THIN = Side(style="thin", color="BFBFBF")


def load_all() -> pd.DataFrame:
    frames = []
    for f in sorted(INPUT.glob("*_매출.xlsx")):
        branch = f.stem.split("_")[0]
        df = pd.read_excel(f)
        df["지점"] = branch
        frames.append(df)
    if not frames:
        raise SystemExit(f"{INPUT} 에 매출 파일이 없습니다.")
    data = pd.concat(frames, ignore_index=True)
    data["일자"] = pd.to_datetime(data["일자"])
    data["월"] = data["일자"].dt.strftime("%Y-%m")
    return data


def build_sheets(data: pd.DataFrame) -> dict[str, pd.DataFrame]:
    monthly = data.pivot_table(index="월", columns="지점", values="금액",
                               aggfunc="sum").reset_index()
    monthly["합계"] = monthly.drop(columns="월").sum(axis=1)

    products = (data.groupby("상품명")
                .agg(판매수량=("수량", "sum"), 매출=("금액", "sum"))
                .sort_values("매출", ascending=False).reset_index())
    products["비중"] = products["매출"] / products["매출"].sum()

    branches = (data.groupby("지점")
                .agg(매출=("금액", "sum"), 판매수량=("수량", "sum"),
                     영업일수=("일자", "nunique"))
                .reset_index())
    branches["일평균매출"] = branches["매출"] / branches["영업일수"]

    raw = data[["일자", "지점", "상품명", "수량", "단가", "금액"]].sort_values(["일자", "지점"])
    return {"월별매출": monthly, "지점별": branches, "상품별": products, "전체내역": raw}


def style(ws, money_cols=(), pct_cols=(), date_cols=()):
    for cell in ws[1]:
        cell.fill, cell.font = HEADER_FILL, HEADER_FONT
        cell.alignment = Alignment(horizontal="center")
    for row in ws.iter_rows(min_row=2):
        for cell in row:
            cell.border = Border(top=THIN, bottom=THIN, left=THIN, right=THIN)
            header = ws.cell(1, cell.column).value
            if header in money_cols:
                cell.number_format = "#,##0"
            elif header in pct_cols:
                cell.number_format = "0.0%"
            elif header in date_cols:
                cell.number_format = "yyyy-mm-dd"
    for i, col in enumerate(ws.columns, 1):
        width = max(len(str(c.value or "")) for c in col)
        ws.column_dimensions[get_column_letter(i)].width = min(max(width * 1.6, 10), 30)
    ws.freeze_panes = "A2"


def add_charts(wb):
    ws = wb["월별매출"]
    n_rows, n_cols = ws.max_row, ws.max_column
    bar = BarChart()
    bar.title, bar.y_axis.title = "월별 지점 매출", "원"
    bar.add_data(Reference(ws, min_col=2, max_col=n_cols - 1, min_row=1, max_row=n_rows),
                 titles_from_data=True)
    bar.set_categories(Reference(ws, min_col=1, min_row=2, max_row=n_rows))
    bar.width, bar.height = 18, 9
    ws.add_chart(bar, f"A{n_rows + 3}")

    ws = wb["상품별"]
    n_rows = ws.max_row
    top = BarChart()
    top.type, top.title = "bar", "상품별 매출"
    top.add_data(Reference(ws, min_col=3, min_row=1, max_row=n_rows), titles_from_data=True)
    top.set_categories(Reference(ws, min_col=1, min_row=2, max_row=n_rows))
    top.width, top.height, top.legend = 16, 9, None
    ws.add_chart(top, "G2")


def main():
    data = load_all()
    OUTPUT.mkdir(exist_ok=True)
    with pd.ExcelWriter(REPORT, engine="openpyxl") as writer:
        for name, df in build_sheets(data).items():
            df.to_excel(writer, sheet_name=name, index=False)

    wb = load_workbook(REPORT)
    money = {"강남점", "홍대점", "부산서면점", "합계", "매출", "일평균매출", "단가", "금액"}
    for ws in wb.worksheets:
        style(ws, money_cols=money, pct_cols={"비중"}, date_cols={"일자"})
    add_charts(wb)
    wb.save(REPORT)
    print(f"파일 {data['지점'].nunique() * data['월'].nunique()}개, "
          f"{len(data):,}건 통합 -> {REPORT}")


if __name__ == "__main__":
    main()
