"""Build Lab 7 report by patching OOXML template; save to docs/Lab7_2312802_PhanKhanhVuong.docx."""
import hashlib
from copy import deepcopy
from pathlib import Path
from zipfile import ZipFile
from lxml import etree

ROOT = Path(__file__).resolve().parents[2]
REFERENCE = ROOT / 'docs/Lab04_2312802_PhanKhanhVuong.docx'

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
    for text, bold in [('Lab: ', False), ('07', True), ('\tTừ ngày: ', False), ('20/10/2026', True), ('\tđến ngày: ', False), ('27/10/2026', True)]:
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
        ('Xây dựng hoàn chỉnh trang xem chi tiết công thức (Món ăn) - Yêu cầu tối thiểu số 4 của Lab 7', [
            'Trực tiếp cài đặt trang xem chi tiết món ăn tại frontend/src/app/recipes/[slug]/page.tsx đáp ứng đầy đủ yêu cầu số 4 của Lab 7 theo phong cách hiện đại Budget Bytes.',
            'Cài đặt tính năng Checklist tương tác: Cho phép người dùng đánh dấu tick chọn từng nguyên liệu đã chuẩn bị sẵn sàng trước khi nấu nướng.',
            'Cài đặt tính năng Đồng hồ bấm giờ đếm ngược (Interactive Step Timer countdown): Tích hợp trực tiếp vào từng bước nấu có quy định thời gian, có các nút Play, Pause, Reset và hiệu ứng chuông hoàn thành trực quan.',
            'Cài đặt khối Thẻ Dinh dưỡng (Nutrition Card) hiển thị chi tiết Calories, Protein, Carbohydrates, Fat, Fiber, Sodium.',
            'Tích hợp Schema SEO có cấu trúc chuẩn quốc tế Recipe JSON-LD (frontend/src/utils/recipeSeo.ts) hỗ trợ tối ưu hiển thị Rich Snippets trên công cụ tìm kiếm Google.',
            'Tích hợp thanh công cụ: In công thức (Print Recipe), Chia sẻ liên kết (Web Share API & Copy Clipboard), và nút tắt nhảy nhanh xuống thẻ công thức chính (Jump to recipe).',
            'Tích hợp thanh quyền tác giả/quản trị viên: Tự động hiển thị nút "Sửa công thức" và "Xóa công thức" kèm Dialog xác nhận xóa mềm khi người dùng đăng nhập là tác giả hoặc Quản trị viên hệ thống.',
        ], [], '100%'),

        ('Tích hợp kết nối API thật cho tính năng Tạo và Xuất bản công thức (/recipes/create)', [
            'Chuyển đổi toàn bộ logic giả lập (mock timeout) sang gọi API Backend C# .NET 10 thực tế: Tích hợp POST /api/v1/recipes (FR-RCP-003) tạo bản nháp với đầy đủ danh sách nguyên liệu và các bước chế biến trong một lệnh duy nhất.',
            'Bổ sung nút và xử lý "Xuất bản ngay" (FR-RCP-007) gọi PUT /api/v1/recipes/{id}/publish sau khi tạo để đưa công thức lên trạng thái công khai ngay lập tức.',
            'Tích hợp tải ảnh đại diện món ăn trực tiếp từ máy tính lên Cloud Supabase Storage (FR-FILE-001) thông qua endpoint POST /api/v1/recipes/{id}/images.',
            'Xử lý bảo mật: Kiểm tra trạng thái xác thực từ Zustand auth-store, hiển thị cảnh báo yêu cầu đăng nhập và chuyển hướng thân thiện.',
            'Tích hợp bộ bắt lỗi chuẩn ProblemDetails RFC 7807 (extractApiErrorMessage) hiển thị thông báo tiếng Việt trực quan khi nhập liệu không hợp lệ.',
        ], [], '100%'),

        ('Xây dựng trang Chỉnh sửa công thức (/recipes/[slug]/edit) kèm Khóa lạc quan OCC', [
            'Trực tiếp xây dựng trang Chỉnh sửa công thức hoàn chỉnh tại frontend/src/app/recipes/[slug]/edit/page.tsx.',
            'Tích hợp cơ chế Khóa Lạc Quan Optimistic Concurrency Control (OCC) (FR-RCP-004): Truyền và kiểm tra mã RowVersion (dạng base64) giữa Frontend và C# Backend nhằm phát hiện và ngăn chặn xung đột ghi đè dữ liệu khi có nhiều phiên chỉnh sửa đồng thời.',
            'Bắt và xử lý chuyên biệt mã lỗi HTTP 409 Conflict: Hiển thị thông báo hướng dẫn người dùng tải lại dữ liệu mới nhất kèm nút "Tải lại công thức" (Refresh).',
            'Hỗ trợ cập nhật linh hoạt danh sách nguyên liệu động, các bước thực hiện có timer và tải mới/thay đổi ảnh đại diện món ăn lên Supabase.',
            'Kiểm soát phân quyền chặt chẽ: Chỉ cho phép tác giả bài viết hoặc tài khoản Quản trị viên (Admin) truy cập trang chỉnh sửa.',
        ], [], '100%'),

        ('Nâng cấp Backend API và CSDL: Hỗ trợ OCC RowVersion & Chuẩn hóa Semantic Category Seeding', [
            'Cập nhật RecipeDetailDto.cs và RecipeRepository.cs: Bổ sung trường RowVersion vào DTO chi tiết công thức để Frontend nhận được concurrency token khi tải chi tiết món.',
            'Mở rộng GetDetailBySlugAsync trong RecipeRepository.cs để hỗ trợ truy vấn linh hoạt bằng cả URL Slug hoặc Recipe Id (GUID), tối ưu hóa trải nghiệm điều hướng.',
            'Chuẩn hóa toàn diện dữ liệu hạt giống (Seed Data): Cập nhật RecipeSeeder.cs và cơ sở dữ liệu PostgreSQL Supabase Cloud, phân loại chính xác 120 món ăn Việt Nam vào đúng 25 danh mục ẩm thực theo ngữ nghĩa thực tế.',
            'Khắc phục xung đột Interface trong Unit Tests (FakeCategoryRepository) sau khi nhánh chính sáp nhập tính năng quản trị danh mục (PR #24).',
        ], [], '100%'),

        ('Kiểm thử chất lượng phần mềm toàn diện (Unit Tests & Frontend Production Build)', [
            'Kiểm thử Backend: Chạy kiểm thử tự động toàn bộ 18/18 Unit Tests trong CulinaryBlog.Application.UnitTests đạt trạng thái Passed 100%.',
            'Kiểm thử Backend Build: Biên dịch dự án Web API .NET 10 (dotnet build CulinaryBlog.API.csproj) đạt 0 Warning(s), 0 Error(s).',
            'Kiểm thử Frontend Typecheck: Lệnh npm run typecheck trên Next.js 15 đạt 0 lỗi TypeScript (0 errors).',
            'Kiểm thử Frontend Production Build: Lệnh npm run build hoàn tất thành công 100%, tạo gói tối ưu cho toàn bộ 14 routes tĩnh và động của hệ thống blog ẩm thực.',
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
        for line in complete:
            prefix = '' if line.startswith('• ') or line.startswith('[') else '• '
            columns[1].append(replace_paragraph(bullet_template, prefix + line))
        if incomplete:
            columns[1].append(replace_paragraph(status_template, 'Chưa hoàn thành:', 'FF0000'))
            for line in incomplete:
                prefix = '' if line.startswith('• ') else '• '
                columns[1].append(replace_paragraph(bullet_template, prefix + line))
        for paragraph in columns[1].findall('w:p', NS):
            text = ''.join(paragraph.itertext())
            if paragraph is columns[1].find('w:p', NS) or text in ['Đã hoàn thành:', 'Chưa hoàn thành:']:
                props = paragraph.find('w:pPr', NS)
                if props is None:
                    props = etree.Element(tag('pPr'))
                    paragraph.insert(0, props)
                etree.SubElement(props, tag('keepNext'))
        columns[2].append(replace_paragraph(cell_templates[2], branch))
        columns[3].append(replace_paragraph(cell_templates[3], progress))
        for element in row.iter():
            for attribute in list(element.attrib):
                if attribute.endswith('}paraId') or attribute.endswith('}textId'): del element.attrib[attribute]
        table.append(row)

    note_template = paragraphs[5]
    note = replace_paragraph(note_template, 'Ghi chú: Tiến độ là ước lượng theo các đầu việc nêu trong bảng. Các phần việc trên tập trung vào phân công module Recipe Core & Media Storage của sinh viên Phan Khánh Vương (MSSV: 2312802) trên branch feature/vuong-recipe-frontend, đáp ứng đầy đủ yêu cầu số 4 của Lab 7 và các kết nối API tương ứng.')
    note_props = note.find('w:pPr', NS)
    if note_props is None:
        note_props = etree.Element(tag('pPr'))
        note.insert(0, note_props)
    spacing = note_props.find('w:spacing', NS)
    if spacing is None: spacing = etree.SubElement(note_props, tag('spacing'))
    spacing.set(tag('before'), '120')
    body.insert(list(body).index(table) + 1, note)
    content = etree.tostring(root, xml_declaration=True, encoding='UTF-8', standalone=True)
    output = ROOT / 'docs/Lab7_2312802_PhanKhanhVuong.docx'
    temp_output = ROOT / 'docs/Lab7_2312802_PhanKhanhVuong_Temp.docx'
    with ZipFile(temp_output, 'w') as target:
        for entry in source.infolist():
            target.writestr(entry, content if entry.filename == 'word/document.xml' else source.read(entry.filename))

    try:
        import shutil
        shutil.move(temp_output, output)
        print(f"Generated successfully: {output}")
    except PermissionError:
        print(f"File {output} dang mo trong Word! Da luu ban moi nhat tai: {temp_output}")

print("Done Lab 7 Report Generation.")
