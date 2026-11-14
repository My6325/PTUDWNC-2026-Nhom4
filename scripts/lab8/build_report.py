"""Build Lab 8 report by patching OOXML template; save to docs/Lab8_2312802_PhanKhanhVuong.docx."""
import shutil
from copy import deepcopy
from pathlib import Path
from zipfile import ZipFile
from lxml import etree

ROOT = Path(__file__).resolve().parents[2]
REFERENCE = ROOT / 'docs/Lab07_2312802_PhanKhanhVuong.docx'

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
    for text, bold in [('Lab: ', False), ('08', True), ('\tTừ ngày: ', False), ('27/10/2026', True), ('\tđến ngày: ', False), ('03/11/2026', True)]:
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
        ('Xây dựng hoàn chỉnh Trang Quản trị Công thức Nấu ăn dành riêng cho Admin (/admin/recipes)', [
            'Xây dựng giao diện Dashboard quản lý công thức toàn diện tại frontend/src/app/admin/recipes/page.tsx dành riêng cho tài khoản Quản trị viên (Admin).',
            'Hệ thống bảo vệ Admin Guard: Kiểm tra phiên đăng nhập và quyền Admin nghiêm ngặt cả ở Frontend UI lẫn Backend Minimal API (RequireAuthorization("Admin")).',
            'Bộ lọc đa trạng thái: Phân chia tab trực quan (Tất cả, Đã xuất bản, Bản nháp, Thùng rác) kèm tổng số lượng công thức thực tế.',
            'Tìm kiếm & Phân trang: Tích hợp ô tìm kiếm tự động debounce 400ms theo tên món ăn và phân trang 12 công thức/trang.',
            'Endpoint Backend GET /api/v1/recipes/admin/all: Hỗ trợ truy vấn danh sách công thức tổng thể, lọc theo trạng thái, cờ xóa mềm và từ khóa tìm kiếm.',
            'Thao tác quản trị trực tiếp trên bảng: Nút Xuất bản, Thu hồi về nháp, Xem chi tiết, Chỉnh sửa, và Chuyển vào thùng rác (Soft Delete) kèm modal xác nhận an toàn.',
        ], [
            'Chưa có tính năng chọn hàng loạt (Bulk selection) để xóa hoặc đổi trạng thái nhiều công thức cùng một lúc.',
        ], '100%'),

        ('Hoàn thiện chu trình Two-Stage Deletion: Thùng rác, Khôi phục & Xóa vĩnh viễn kèm dọn ảnh Supabase', [
            'Sửa triệt để lỗi hiển thị ảnh đại diện cho các công thức trong Thùng rác (hiển thị ảnh bìa rõ ràng, không bị placeholder rỗng).',
            'Hiện thực tính năng Khôi phục công thức từ Thùng rác (Restore): Bổ sung RestoreRecipeCommand và API endpoint POST /api/v1/recipes/{id}/restore, cascade khôi phục toàn bộ ảnh liên quan.',
            'Hiện thực tính năng Xóa luôn (Hard Delete): Bổ sung endpoint DELETE /api/v1/recipes/{id}/permanent dành riêng cho Admin, xóa sạch bản ghi khỏi CSDL PostgreSQL.',
            'Dọn dẹp vật lý trên Supabase Storage (FR-FILE-002): Khi bấm Xóa luôn, hệ thống tự động quét và xóa sạch các file ảnh vật lý (OriginalUrl, MediumUrl, ThumbnailUrl) trên Cloud Supabase Storage, tránh lãng phí dung lượng CDN.',
            'Giao diện Thùng rác hoàn thiện: Bổ sung nút "Xóa luôn" màu đỏ cạnh nút "Khôi phục" với hộp thoại modal cảnh báo nguy hiểm không thể phục hồi.',
        ], [], '100%'),

        ('Hiện thực tác vụ nền tự động Resize ảnh (FR-JOB-002) bằng SixLabors.ImageSharp', [
            'Tích hợp thư viện xử lý ảnh chuyên dụng SixLabors.ImageSharp vào dự án Web API .NET 10.',
            'Hiện thực Background Runner bất đồng bộ cho ImageResizeJob: Khi tải ảnh lên, hệ thống trả kết quả ảnh gốc ngay lập tức; luồng nền tự động nén WebP và sinh 2 kích thước chuẩn:',
            '  + Phiên bản Thumbnail (300×300): Crop vuông trung tâm tối ưu hóa cho avatar, card danh sách và bảng quản trị.',
            '  + Phiên bản Medium (800×600): Giữ nguyên tỷ lệ chuẩn cho trang chi tiết công thức.',
            'Tự động upload ảnh đã resize lên bucket culinary-blog trên Supabase Storage và cập nhật lại MediumUrl, ThumbnailUrl vào bản ghi RecipeImage trong CSDL PostgreSQL.',
        ], [], '100%'),

        ('Chuẩn hóa chất lượng dữ liệu công thức món ăn & Cập nhật ảnh Supabase Storage CDN', [
            'Chuẩn hóa toàn diện nội dung công thức món ăn Việt Nam, khắc phục lỗi gán nhầm catalog giữa các món (Tôm rim mặn ngọt, Lẩu thái hải sản, Sườn xào chua ngọt).',
            'Loại bỏ ảnh bước nấu không phù hợp: Xóa bỏ toàn bộ ảnh stock người nước ngoài trong Bước 1 của các công thức (ImageUrl = null), giúp giao diện tập trung vào tên bước, hướng dẫn và bộ đếm giờ (Timer).',
            'Cập nhật đường dẫn ảnh đại diện thực tế từ CDN Supabase Storage cho toàn bộ các công thức trong hệ thống.',
        ], [], '100%'),

        ('Kiểm thử tự động, Build hệ thống & Đẩy Pull Request lên nhánh Git', [
            'Kiểm thử Backend tự động: Chạy toàn bộ 112/112 bài kiểm thử (18 Unit Tests + 94 Integration Tests) đạt kết quả PASSED 100%.',
            'Bổ sung phương thức giả lập mặc định trên IRecipeRepository đảm bảo toàn bộ pipeline test không bị lỗi sau khi thêm tính năng Admin.',
            'Kiểm tra biên dịch dự án Web API .NET 10 thành công với 0 Error.',
            'Kiểm tra kiểu dữ liệu Frontend Next.js 15 (npm run typecheck) đạt 0 lỗi TypeScript.',
            'Đẩy toàn bộ 10 commit sạch lên nhánh feature/vuong-recipe-frontend và tạo Pull Request sáp nhập vào main với trạng thái "No conflicts with base branch".',
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
            prefix = '' if line.startswith('• ') or line.startswith('[') or line.startswith('  +') else '• '
            columns[1].append(replace_paragraph(bullet_template, prefix + line))
        if incomplete:
            columns[1].append(replace_paragraph(status_template, 'Chưa làm được:', 'FF0000'))
            for line in incomplete:
                prefix = '' if line.startswith('• ') else '• '
                columns[1].append(replace_paragraph(bullet_template, prefix + line))
        for paragraph in columns[1].findall('w:p', NS):
            text = ''.join(paragraph.itertext())
            if paragraph is columns[1].find('w:p', NS) or text in ['Đã hoàn thành:', 'Chưa làm được:']:
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
    note = replace_paragraph(note_template, 'Ghi chú: Tiến độ là ước lượng theo các đầu việc nêu trong bảng. Các phần việc trên phản ánh chính xác các nội dung thực hiện trong tuần Lab 8 (27/10/2026 - 03/11/2026) của sinh viên Phan Khánh Vương (MSSV: 2312802) trên branch feature/vuong-recipe-frontend, tập trung vào hoàn thiện Quản trị Admin, chu trình Thùng rác & Xóa luôn, tác vụ nền Resize ảnh và kiểm thử hệ thống.')
    note_props = note.find('w:pPr', NS)
    if note_props is None:
        note_props = etree.Element(tag('pPr'))
        note.insert(0, note_props)
    spacing = note_props.find('w:spacing', NS)
    if spacing is None: spacing = etree.SubElement(note_props, tag('spacing'))
    spacing.set(tag('before'), '120')
    body.insert(list(body).index(table) + 1, note)
    content = etree.tostring(root, xml_declaration=True, encoding='UTF-8', standalone=True)
    output = ROOT / 'docs/Lab8_2312802_PhanKhanhVuong.docx'
    temp_output = ROOT / 'docs/Lab8_2312802_PhanKhanhVuong_Temp.docx'
    with ZipFile(temp_output, 'w') as target:
        for entry in source.infolist():
            target.writestr(entry, content if entry.filename == 'word/document.xml' else source.read(entry.filename))

    try:
        shutil.move(temp_output, output)
        print(f"Generated successfully: {output}")
    except PermissionError:
        print(f"File {output} dang mo trong Word! Da luu ban moi nhat tai: {temp_output}")
