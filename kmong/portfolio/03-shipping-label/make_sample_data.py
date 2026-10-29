"""포트폴리오용 가짜 주문 데이터. 이름·번호·주소는 모두 지어낸 값이다."""
import random
from pathlib import Path

import pandas as pd

random.seed(3)

NAMES = ["김하늘", "이도윤", "박서연", "최지호", "정유나", "강민준", "조수아", "윤시우",
         "장예린", "임건우", "한소율", "오태윤"]
ADDRS = ["서울시 마포구 샘플로 12, 101동 202호", "경기도 성남시 분당구 예시길 45",
         "부산시 해운대구 테스트대로 7, 3층", "인천시 연수구 가상로 88",
         "대전시 유성구 견본길 23-4", "광주시 서구 모의로 150"]
PRODUCTS = ["수제 그래놀라 500g", "꿀 땅콩버터 350g", "현미 누룽지 1kg", "유기농 아몬드 300g"]
MEMOS = [None, None, "문 앞에 놓아주세요", "경비실에 맡겨주세요", "배송 전 연락 부탁드려요"]
PHONE_FORMATS = ["010-{a}-{b}", "010{a}{b}", "010 {a} {b}", "+82 10-{a}-{b}"]

customers = []
for name in NAMES:
    a, b = f"{random.randint(0, 9999):04d}", f"{random.randint(0, 9999):04d}"
    customers.append((name, random.choice(PHONE_FORMATS).format(a=a, b=b),
                      random.choice(ADDRS), random.choice(MEMOS)))

rows = []
for i in range(30):
    name, phone, addr, memo = random.choice(customers)
    rows.append({
        "주문번호": 2026102900 + i,
        "주문상태": random.choices(["결제완료", "취소", "반품"], [18, 1, 1])[0],
        "수취인": name,
        "수취인연락처": phone,
        "배송지": addr if random.random() > 0.05 else None,
        "상품명": random.choice(PRODUCTS),
        "수량": random.randint(1, 3),
        "배송메모": memo,
    })

out = Path(__file__).parent / "input"
out.mkdir(exist_ok=True)
pd.DataFrame(rows).to_excel(out / "주문내역.xlsx", index=False)
print(f"주문 {len(rows)}건 생성")
