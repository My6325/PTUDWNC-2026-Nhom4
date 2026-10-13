"""Build Lab 5 by patching the Lab 4 OOXML template; preserve other package parts."""
import hashlib
import json
from copy import deepcopy
from pathlib import Path
from zipfile import ZipFile
from lxml import etree

ROOT = Path(__file__).resolve().parents[2]
REFERENCE = ROOT / 'docs/Lab04_2312802_PhanKhanhVuong.docx'
EXPECTED = '32860e25ae8b0b8c0653b220c9cea1539e4016dd2231152e223c7a2300b3ce6f'
assert hashlib.sha256(REFERENCE.read_bytes()).hexdigest() == EXPECTED
EVIDENCE = ROOT / 'docs/evidence/lab5'
results = json.loads((EVIDENCE / 'local-test-results.json').read_text(encoding='utf-8-sig'))
assert all(r['outcome'] == 'Passed' for r in results)
assert json.loads((EVIDENCE / 'frontend-http.json').read_text(encoding='utf-8-sig'))['status'] == 200
passed = len(results)
lab5 = sum('Lab5RecipeSecurityTests' in r['testName'] for r in results)
W = 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'
NS = {'w': W}
def tag(name): return '{' + W + '}' + name

with ZipFile(REFERENCE) as source:
    root = etree.fromstring(source.read('word/document.xml'))
    body = root.find('w:body', NS)
    paragraphs = body.findall('w:p', NS)
    meta = paragraphs[2]
    meta_run = deepcopy(meta.find('w:r', NS).find('w:rPr', NS))
    for child in list(meta):
        if child.tag != tag('pPr'): meta.remove(child)
    for text, bold in [('Lab: ', False), ('05', True), ('\tTừ ngày: ', False), ('06/10/2026', True), ('\tđến ngày: ', False), ('13/10/2026', True)]:
        run = etree.SubElement(meta, tag('r'))
        props = deepcopy(meta_run)
        for b in props.findall('w:b', NS): props.remove(b)
        if bold: etree.SubElement(props, tag('b'))
        run.append(props)
        pieces = text.split('\t')
        for i, piece in enumerate(pieces):
            if i: etree.SubElement(run, tag('tab'))
            if piece:
                t = etree.SubElement(run, tag('t'))
                t.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
                t.text = piece

    table = body.find('w:tbl', NS)
    rows = table.findall('w:tr', NS)
    template = deepcopy(rows[1])
    cells = template.findall('w:tc', NS)
    work_paragraphs = cells[1].findall('w:p', NS)
    heading_template, status_template, bullet_template = work_paragraphs[:3]
    for row in rows[1:]: table.remove(row)
    header = rows[0]
    trpr = header.find('w:trPr', NS)
    if trpr is None: trpr = etree.SubElement(header, tag('trPr'))
    etree.SubElement(trpr, tag('tblHeader'))

    tasks = [
        ('Bảo mật CORS, Rate Limiting và Authentication Authorization', [
            'Kế thừa JWT, CORS và Rate Limiting dùng chung của nhóm; kiểm chứng trên API Recipe.',
            'CORS cho localhost:3000; origin ngoài danh sách không nhận header cho phép.',
            'Giới hạn 60 request/phút/IP, hàng đợi 2; kiểm thử vượt giới hạn trả HTTP 429.',
            'JWT và quyền sở hữu: không token/token lỗi trả 401; người khác trả 403; tác giả hoặc Admin cập nhật thành công với HTTP 200.',
            'Bổ sung test handler phân quyền thật và HTTP Kestrel local; dùng JWT thử nghiệm, repository trong bộ nhớ.',
        ], ['Chưa nghiệm thu toàn bộ ứng dụng với tài khoản Identity và Supabase thật.'], '80%'),
        ('Tài liệu đặc tả API và API versioning', [
            'Kiểm chứng OpenAPI có Bearer JWT, schema request/response và các mã lỗi của API Recipe.',
            'Bổ sung metadata UpdateRecipe, mô tả RowVersion Base64 và response 400 cho upload ảnh.',
            'Kiểm chứng multipart/form-data và Scalar trả HTML 200 trên backend local.',
        ], ['API hiện chỉ có tiền tố /api/v1; cơ chế quản lý phiên bản đầy đủ cần phối hợp nhóm.', 'Chưa kiểm chứng tương tác JavaScript của Scalar trong trình duyệt.'], '50%'),
        ('Cài đặt môi trường và cấu trúc dự án front end', [
            'Bổ sung frontend/.env.example với NEXT_PUBLIC_API_URL=http://localhost:5000/api/v1.',
            'Chuẩn bị components/recipes và hooks bằng .gitkeep; có app, services và types.',
            'Bổ sung script typecheck; kiểm tra TypeScript và Next.js build thành công.',
            'Chạy front-end ở cổng 3000, xác nhận HTTP 200 và HTML chứa Culinary Blog.',
            'Giao diện tạo/sửa công thức hoàn chỉnh thuộc giai đoạn tiếp theo, ngoài yêu cầu tối thiểu Lab 5.',
        ], [], '100%'),
        ('Cài đặt các thư viện cần thiết', [
            'Chạy npm ci thành công với 424 packages; giữ nguyên phiên bản theo package-lock.json.',
            'Sử dụng Next.js, React, TypeScript, Tailwind CSS, Axios, React Hook Form và Zod đã khai báo.',
            'Không thêm thư viện mới cho phần types và service; Next.js 15.5.27 build thành công.',
        ], [], '100%'),
        ('Cài đặt TS types và service cho module Recipe', [
            'Tạo recipe.ts: công thức, request tạo/cập nhật, dinh dưỡng, bước nấu, nguyên liệu và ảnh.',
            'Enum độ khó Easy=1, Medium=2, Hard=3; trạng thái Draft=1, Published=2, Archived=3 khớp backend.',
            'GUID và RowVersion Base64 dùng string; trường nullable dùng null, tên JSON camelCase.',
            'Bổ sung 11 thao tác Axios service có types; xóa trả void, upload FormData với trường file và không dùng header JSON.',
            f'Kiểm tra 11 thao tác service đạt; bộ test local đạt {passed}/{passed}, gồm {lab5} trường hợp Lab 5.',
            'Minh chứng: docs/evidence/lab5; không chạy migration/seed hoặc thao tác storage thật.',
        ], [], '100%'),
    ]

    def replace_paragraph(template_p, text, color=None):
        p = deepcopy(template_p)
        run_template = p.find('.//w:r', NS)
        props = deepcopy(run_template.find('w:rPr', NS)) if run_template is not None else None
        for child in list(p):
            if child.tag != tag('pPr'): p.remove(child)
        run = etree.SubElement(p, tag('r'))
        if props is not None:
            if color:
                for c in props.findall('w:color', NS): props.remove(c)
                c = etree.SubElement(props, tag('color'))
                c.set(tag('val'), color)
            run.append(props)
        t = etree.SubElement(run, tag('t'))
        t.text = text
        return p

    branch = 'https://github.com/My6325/PTUDWNC-2026-Nhom4/tree/feature/vuong-recipe-frontend'
    for number, (title, complete, incomplete, progress) in enumerate(tasks, 1):
        row = deepcopy(template)
        row_props = row.find('w:trPr', NS)
        if row_props is None:
            row_props = etree.Element(tag('trPr'))
            row.insert(0, row_props)
        if row_props.find('w:cantSplit', NS) is None:
            etree.SubElement(row_props, tag('cantSplit'))
        columns = row.findall('w:tc', NS)
        cell_templates = [deepcopy(c.find('w:p', NS)) for c in columns]
        for cell in columns:
            for child in list(cell):
                if child.tag != tag('tcPr'): cell.remove(child)
        columns[0].append(replace_paragraph(cell_templates[0], str(number)))
        columns[1].append(replace_paragraph(heading_template, title))
        columns[1].append(replace_paragraph(status_template, 'Đã hoàn thành:', '4FAA2C'))
        for line in complete: columns[1].append(replace_paragraph(bullet_template, '• ' + line))
        if incomplete:
            columns[1].append(replace_paragraph(status_template, 'Chưa hoàn thành:', 'FF0000'))
            for line in incomplete: columns[1].append(replace_paragraph(bullet_template, '• ' + line))
        for paragraph in columns[1].findall('w:p', NS):
            text = ''.join(paragraph.itertext())
            if paragraph is columns[1].find('w:p', NS) or text in ['Đã hoàn thành:', 'Chưa hoàn thành:']:
                props = paragraph.find('w:pPr', NS)
                if props is None:
                    props = etree.Element(tag('pPr'))
                    paragraph.insert(0, props)
                etree.SubElement(props, tag('keepNext'))
        branch_text = 'Cùng branch với công việc 1: feature/vuong-recipe-frontend' if number == 4 else branch
        columns[2].append(replace_paragraph(cell_templates[2], branch_text))
        columns[3].append(replace_paragraph(cell_templates[3], progress))
        # Cloned paragraph identifiers must not collide.
        for element in row.iter():
            for attribute in list(element.attrib):
                if attribute.endswith('}paraId') or attribute.endswith('}textId'): del element.attrib[attribute]
        table.append(row)

    note_template = paragraphs[5]
    note = replace_paragraph(note_template, 'Ghi chú: Tiến độ là ước lượng theo các đầu việc nêu trong bảng. Các thay đổi được quản lý trên branch feature/vuong-recipe-frontend. Kết quả kiểm thử local không thay thế nghiệm thu Supabase thật.')
    note_props = note.find('w:pPr', NS)
    if note_props is None:
        note_props = etree.Element(tag('pPr'))
        note.insert(0, note_props)
    spacing = note_props.find('w:spacing', NS)
    if spacing is None: spacing = etree.SubElement(note_props, tag('spacing'))
    spacing.set(tag('before'), '120')
    body.insert(list(body).index(table) + 1, note)
    content = etree.tostring(root, xml_declaration=True, encoding='UTF-8', standalone=True)
    output = ROOT / 'docs/Lab5_2312802_PhanKhanhVuong.docx'
    with ZipFile(output, 'w') as target:
        for entry in source.infolist():
            target.writestr(entry, content if entry.filename == 'word/document.xml' else source.read(entry.filename))

assert hashlib.sha256(REFERENCE.read_bytes()).hexdigest() == EXPECTED
with ZipFile(REFERENCE) as baseline, ZipFile(output) as final:
    assert baseline.namelist() == final.namelist()
    assert all(baseline.read(name) == final.read(name) for name in baseline.namelist() if name != 'word/document.xml')
print(output)
print('Template package preservation verified; only word/document.xml changed.')
