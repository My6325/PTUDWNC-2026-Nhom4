import fs from 'node:fs';
import path from 'node:path';
import { execSync } from 'node:child_process';

const outputDir = path.resolve('docs');
const tempDir = path.resolve('temp_docx_build');
const outputDocx = path.join(outputDir, 'BaoCao_DoAn_PTUDWNC_KhungMau.docx');

if (!fs.existsSync(outputDir)) {
  fs.mkdirSync(outputDir, { recursive: true });
}

if (fs.existsSync(tempDir)) {
  fs.rmSync(tempDir, { recursive: true, force: true });
}

fs.mkdirSync(path.join(tempDir, '_rels'), { recursive: true });
fs.mkdirSync(path.join(tempDir, 'word', '_rels'), { recursive: true });

function escapeXml(unsafe) {
  if (unsafe == null) return '';
  return String(unsafe)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');
}

// 1. [Content_Types].xml
const contentTypesXml = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
</Types>`;
fs.writeFileSync(path.join(tempDir, '[Content_Types].xml'), contentTypesXml, 'utf8');

// 2. _rels/.rels
const packageRelsXml = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>`;
fs.writeFileSync(path.join(tempDir, '_rels', '.rels'), packageRelsXml, 'utf8');

// 3. word/_rels/document.xml.rels
const docRelsXml = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rIdStyles" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
</Relationships>`;
fs.writeFileSync(path.join(tempDir, 'word', '_rels', 'document.xml.rels'), docRelsXml, 'utf8');

// 4. word/styles.xml
const stylesXml = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:docDefaults>
    <w:rPrDefault>
      <w:rPr>
        <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman"/>
        <w:sz w:val="26"/>
        <w:szCs w:val="26"/>
        <w:lang w:val="vi-VN"/>
      </w:rPr>
    </w:rPrDefault>
    <w:pPrDefault>
      <w:pPr>
        <w:spacing w:line="312" w:lineRule="auto" w:after="120"/>
      </w:pPr>
    </w:pPrDefault>
  </w:docDefaults>

  <w:style w:type="paragraph" w:default="1" w:styleId="Normal">
    <w:name w:val="Normal"/>
    <w:qFormat/>
  </w:style>

  <w:style w:type="paragraph" w:styleId="Heading1">
    <w:name w:val="heading 1"/>
    <w:basedOn w:val="Normal"/>
    <w:next w:val="Normal"/>
    <w:qFormat/>
    <w:pPr>
      <w:keepNext/>
      <w:spacing w:before="360" w:after="180" w:line="360" w:lineRule="auto"/>
      <w:jc w:val="left"/>
    </w:pPr>
    <w:rPr>
      <w:b/>
      <w:sz w:val="30"/>
      <w:szCs w:val="30"/>
      <w:color w:val="0F172A"/>
    </w:rPr>
  </w:style>

  <w:style w:type="paragraph" w:styleId="Heading2">
    <w:name w:val="heading 2"/>
    <w:basedOn w:val="Normal"/>
    <w:next w:val="Normal"/>
    <w:qFormat/>
    <w:pPr>
      <w:keepNext/>
      <w:spacing w:before="240" w:after="120" w:line="320" w:lineRule="auto"/>
      <w:jc w:val="left"/>
    </w:pPr>
    <w:rPr>
      <w:b/>
      <w:sz w:val="28"/>
      <w:szCs w:val="28"/>
      <w:color w:val="1E293B"/>
    </w:rPr>
  </w:style>

  <w:style w:type="paragraph" w:styleId="Heading3">
    <w:name w:val="heading 3"/>
    <w:basedOn w:val="Normal"/>
    <w:next w:val="Normal"/>
    <w:qFormat/>
    <w:pPr>
      <w:keepNext/>
      <w:spacing w:before="180" w:after="80" w:line="300" w:lineRule="auto"/>
      <w:jc w:val="left"/>
    </w:pPr>
    <w:rPr>
      <w:b/>
      <w:i/>
      <w:sz w:val="26"/>
      <w:szCs w:val="26"/>
      <w:color w:val="334155"/>
    </w:rPr>
  </w:style>
</w:styles>`;
fs.writeFileSync(path.join(tempDir, 'word', 'styles.xml'), stylesXml, 'utf8');

// Helper XML builders
function p(text = '', opts = {}) {
  const {
    style,
    align,
    bold,
    italic,
    size,
    color,
    spacingBefore,
    spacingAfter,
    lineSpacing,
    indentLeft
  } = opts;

  let pPr = '';
  if (style) pPr += `<w:pStyle w:val="${style}"/>`;
  if (align) pPr += `<w:jc w:val="${align}"/>`;

  let spacing = '';
  if (spacingBefore != null) spacing += ` w:before="${spacingBefore}"`;
  if (spacingAfter != null) spacing += ` w:after="${spacingAfter}"`;
  if (lineSpacing != null) spacing += ` w:line="${lineSpacing}" w:lineRule="auto"`;
  if (spacing) pPr += `<w:spacing${spacing}/>`;

  if (indentLeft != null) pPr += `<w:ind w:left="${indentLeft}"/>`;

  let rPr = '';
  if (bold) rPr += '<w:b/>';
  if (italic) rPr += '<w:i/>';
  if (size) rPr += `<w:sz w:val="${size}"/><w:szCs w:val="${size}"/>`;
  if (color) rPr += `<w:color w:val="${color}"/>`;

  return `<w:p>${pPr ? `<w:pPr>${pPr}</w:pPr>` : ''}<w:r>${rPr ? `<w:rPr>${rPr}</w:rPr>` : ''}<w:t xml:space="preserve">${escapeXml(text)}</w:t></w:r></w:p>`;
}

function pageBreak() {
  return '<w:p><w:r><w:br w:type="page"/></w:r></w:p>';
}

function codeBlock(codeLines) {
  return codeLines.map(line => `
    <w:p>
      <w:pPr>
        <w:pBdr>
          <w:left w:val="single" w:sz="18" w:space="8" w:color="3B82F6"/>
        </w:pBdr>
        <w:shd w:val="clear" w:color="auto" w:fill="F8FAFC"/>
        <w:spacing w:before="40" w:after="40" w:line="240" w:lineRule="auto"/>
        <w:ind w:left="360" w:right="180"/>
      </w:pPr>
      <w:r>
        <w:rPr>
          <w:rFonts w:ascii="Consolas" w:hAnsi="Consolas" w:cs="Consolas"/>
          <w:sz w:val="21"/>
          <w:szCs w:val="21"/>
          <w:color w:val="0F172A"/>
        </w:rPr>
        <w:t xml:space="preserve">${escapeXml(line)}</w:t>
      </w:r>
    </w:p>
  `).join('\n');
}

function imagePlaceholder(caption, figNum = '') {
  return `
    <w:p>
      <w:pPr>
        <w:pBdr>
          <w:top w:val="dashed" w:sz="6" w:space="12" w:color="94A3B8"/>
          <w:left w:val="dashed" w:sz="6" w:space="12" w:color="94A3B8"/>
          <w:bottom w:val="dashed" w:sz="6" w:space="12" w:color="94A3B8"/>
          <w:right w:val="dashed" w:sz="6" w:space="12" w:color="94A3B8"/>
        </w:pBdr>
        <w:shd w:val="clear" w:color="auto" w:fill="F1F5F9"/>
        <w:spacing w:before="180" w:after="120" w:line="360" w:lineRule="auto"/>
        <w:ind w:left="720" w:right="720"/>
        <w:jc w:val="center"/>
      </w:pPr>
      <w:r>
        <w:rPr>
          <w:i/>
          <w:color w:val="64748B"/>
          <w:sz w:val="24"/>
          <w:szCs w:val="24"/>
        </w:rPr>
        <w:t xml:space="preserve">[ Chèn hình ảnh minh họa tại đây ]</w:t>
      </w:r>
    </w:p>
    <w:p>
      <w:pPr>
        <w:spacing w:before="60" w:after="240"/>
        <w:jc w:val="center"/>
      </w:pPr>
      <w:r>
        <w:rPr>
          <w:i/>
          <w:b/>
          <w:sz w:val="24"/>
          <w:szCs w:val="24"/>
          <w:color w:val="334155"/>
        </w:rPr>
        <w:t xml:space="preserve">${escapeXml(caption)}</w:t>
      </w:r>
    </w:p>
  `;
}

function emptyTable(headers, colWidths, numEmptyRows = 4) {
  const totalWidth = colWidths.reduce((a, b) => a + b, 0);

  let gridCols = colWidths.map(w => `<w:gridCol w:w="${w}"/>`).join('');

  let headerCells = headers.map((h, i) => `
    <w:tc>
      <w:tcPr>
        <w:tcW w:w="${colWidths[i]}" w:type="dxa"/>
        <w:shd w:val="clear" w:color="auto" w:fill="E2E8F0"/>
        <w:tcMar>
          <w:top w:w="140" w:type="dxa"/>
          <w:bottom w:w="140" w:type="dxa"/>
          <w:left w:w="180" w:type="dxa"/>
          <w:right w:w="180" w:type="dxa"/>
        </w:tcMar>
        <w:vAlign w:val="center"/>
      </w:tcPr>
      <w:p>
        <w:pPr>
          <w:spacing w:before="60" w:after="60"/>
          <w:jc w:val="center"/>
        </w:pPr>
        <w:r>
          <w:rPr>
            <w:b/>
            <w:sz w:val="24"/>
            <w:szCs w:val="24"/>
            <w:color w:val="0F172A"/>
          </w:rPr>
          <w:t xml:space="preserve">${escapeXml(h)}</w:t>
        </w:r>
      </w:p>
    </w:tc>
  `).join('');

  let headerRow = `
    <w:tr>
      <w:trPr>
        <w:tblHeader/>
        <w:cantSplit/>
      </w:trPr>
      ${headerCells}
    </w:tr>
  `;

  let dataRows = '';
  for (let r = 0; r < numEmptyRows; r++) {
    let cells = colWidths.map(w => `
      <w:tc>
        <w:tcPr>
          <w:tcW w:w="${w}" w:type="dxa"/>
          <w:tcMar>
            <w:top w:w="160" w:type="dxa"/>
            <w:bottom w:w="160" w:type="dxa"/>
            <w:left w:w="180" w:type="dxa"/>
            <w:right w:w="180" w:type="dxa"/>
          </w:tcMar>
          <w:vAlign w:val="center"/>
        </w:tcPr>
        <w:p>
          <w:pPr>
            <w:spacing w:before="60" w:after="60"/>
          </w:pPr>
          <w:r>
            <w:rPr>
              <w:sz w:val="24"/>
              <w:szCs w:val="24"/>
            </w:rPr>
            <w:t xml:space="preserve"></w:t>
          </w:r>
        </w:p>
      </w:tc>
    `).join('');

    dataRows += `
      <w:tr>
        <w:trPr>
          <w:cantSplit/>
          <w:trHeight w:val="420" w:hRule="atLeast"/>
        </w:trPr>
        ${cells}
      </w:tr>
    `;
  }

  return `
    <w:tbl>
      <w:tblPr>
        <w:tblW w:w="${totalWidth}" w:type="dxa"/>
        <w:jc w:val="center"/>
        <w:tblBorders>
          <w:top w:val="single" w:sz="6" w:space="0" w:color="0F172A"/>
          <w:left w:val="single" w:sz="6" w:space="0" w:color="0F172A"/>
          <w:bottom w:val="single" w:sz="6" w:space="0" w:color="0F172A"/>
          <w:right w:val="single" w:sz="6" w:space="0" w:color="0F172A"/>
          <w:insideH w:val="single" w:sz="4" w:space="0" w:color="CBD5E1"/>
          <w:insideV w:val="single" w:sz="4" w:space="0" w:color="CBD5E1"/>
        </w:tblBorders>
      </w:tblPr>
      <w:tblGrid>
        ${gridCols}
      </w:tblGrid>
      ${headerRow}
      ${dataRows}
    </w:tbl>
  `;
}

// Assemble full document body
const docBody = [
  // --- TRANG BÌA (COVER PAGE) ---
  p('TRƯỜNG ĐẠI HỌC ĐÀ LẠT', { align: 'center', bold: true, size: 28, spacingBefore: 0, spacingAfter: 60 }),
  p('KHOA CÔNG NGHỆ THÔNG TIN', { align: 'center', bold: true, size: 28, spacingBefore: 0, spacingAfter: 80 }),
  p('---oOo---', { align: 'center', size: 24, spacingBefore: 0, spacingAfter: 360 }),

  // Khung biểu trưng logo (xóa ảnh, thay bằng khung văn bản trang nhã)
  p('[ KHUNG BIỂU TRƯNG / LOGO ĐẠI HỌC ĐÀ LẠT ]', { align: 'center', italic: true, color: '64748B', size: 24, spacingBefore: 240, spacingAfter: 360 }),

  p('BÁO CÁO ĐỒ ÁN MÔN HỌC', { align: 'center', bold: true, size: 36, color: '0F172A', spacingBefore: 400, spacingAfter: 120 }),
  p('CULINARY BLOG', { align: 'center', bold: true, size: 36, color: 'EA580C', spacingBefore: 80, spacingAfter: 80 }),
  p('Blog Ẩm thực và Nấu ăn', { align: 'center', bold: true, italic: true, size: 28, color: '334155', spacingBefore: 0, spacingAfter: 600 }),

  p('Môn học:              Phát triển ứng dụng web nâng cao', { indentLeft: 1800, size: 26, spacingBefore: 60, spacingAfter: 60 }),
  p('Giáo viên phụ trách:  Nguyễn Duy Khánh', { indentLeft: 1800, size: 26, spacingBefore: 60, spacingAfter: 60 }),
  p('Nhóm thực hiện:       Nhóm 4 (PTUDWNC)', { indentLeft: 1800, bold: true, size: 26, spacingBefore: 60, spacingAfter: 60 }),
  p('Sinh viên thực hiện:', { indentLeft: 1800, bold: true, size: 26, spacingBefore: 120, spacingAfter: 40 }),
  p('1. 2312693 – Nguyễn Thị Trà My (Nhóm trưởng)', { indentLeft: 2200, size: 26, spacingBefore: 40, spacingAfter: 40 }),
  p('2. 2312664 – Hoàng Trịnh Việt Linh', { indentLeft: 2200, size: 26, spacingBefore: 40, spacingAfter: 40 }),
  p('3. 2312802 – Phan Khánh Vương', { indentLeft: 2200, size: 26, spacingBefore: 40, spacingAfter: 40 }),
  p('4. 2312597 – Lê Phạm Mi Đoan', { indentLeft: 2200, size: 26, spacingBefore: 40, spacingAfter: 800 }),

  p('Đà Lạt, tháng 11 năm 2026', { align: 'center', italic: true, size: 26, spacingBefore: 800, spacingAfter: 0 }),
  pageBreak(),

  // --- CHƯƠNG 1 ---
  p('CHƯƠNG 1: BẢNG PHÂN CÔNG VIỆC VÀ KẾT QUẢ THỰC HIỆN', { style: 'Heading1', bold: true, align: 'left', size: 30 }),
  
  p('1.1. Bảng phân công công việc', { style: 'Heading2', bold: true, size: 28 }),
  p('Bảng 1.1. Bảng phân công công việc theo module.', { italic: true, align: 'center', size: 24, spacingBefore: 60, spacingAfter: 120 }),
  emptyTable(
    ['MSSV', 'Thành viên', 'Module chức năng', 'Nội dung công việc thực hiện'],
    [1500, 2200, 2200, 3171],
    4 // 4 dòng trống cho 4 thành viên
  ),
  p('', { spacingAfter: 240 }),

  p('1.2. Kết quả đạt được theo từng yêu cầu', { style: 'Heading2', bold: true, size: 28 }),
  p('Bảng 1.2. Kết quả thực hiện theo từng yêu cầu', { italic: true, align: 'center', size: 24, spacingBefore: 60, spacingAfter: 120 }),
  emptyTable(
    ['Mã công việc', 'Công việc', 'Người phụ trách', 'Trạng thái', 'Bằng chứng / Ghi chú'],
    [1500, 2400, 1600, 1400, 2171],
    12 // 12 dòng trống cho các module/tính năng
  ),
  pageBreak(),

  // --- CHƯƠNG 2 ---
  p('CHƯƠNG 2: HÌNH ẢNH SẢN PHẨM', { style: 'Heading1', bold: true, align: 'left', size: 30 }),
  p('Lưu ý: Toàn bộ ảnh chụp màn hình minh họa chức năng được chuẩn bị khung sẵn dưới đây. Thành viên chỉ cần dán ảnh chụp thực tế vào từng khung tương ứng.', { italic: true, color: '64748B', size: 24, spacingAfter: 200 }),

  p('2.1. Các màn hình chung', { style: 'Heading2', bold: true, size: 28 }),
  imagePlaceholder('Hình 2.1. Màn hình chính 1'),
  imagePlaceholder('Hình 2.2. Màn hình chính 2'),

  p('2.2. Các màn hình Module FR-AUTH', { style: 'Heading2', bold: true, size: 28 }),
  imagePlaceholder('Hình 2.3. Màn hình đăng ký tài khoản'),
  imagePlaceholder('Hình 2.4. Màn hình xác nhận tài khoản mới'),
  imagePlaceholder('Hình 2.5. Email xác nhận tài khoản'),
  imagePlaceholder('Hình 2.6. Màn hình sau nhấn link xác nhận'),
  imagePlaceholder('Hình 2.7. Màn hình sau khi kích hoạt thành công'),
  imagePlaceholder('Hình 2.8. Màn hình hồ sơ cá nhân'),
  imagePlaceholder('Hình 2.9. Màn hình hồ sơ cá nhân sau cập nhật'),
  imagePlaceholder('Hình 2.10. Màn hình đổi mật khẩu'),
  imagePlaceholder('Hình 2.11. Màn hình quên mật khẩu'),
  imagePlaceholder('Hình 2.12. Email đặt lại mật khẩu'),
  imagePlaceholder('Hình 2.13. Đăng ký/Đăng nhập tài khoản bằng Google (Bước chọn tài khoản)'),
  imagePlaceholder('Hình 2.14. Đăng ký/Đăng nhập tài khoản bằng Google (Bước cấp quyền)'),
  imagePlaceholder('Hình 2.15. Kết quả sau Đăng ký/Đăng nhập tài khoản bằng Google'),

  p('2.3. Các màn hình Module FR-CAT', { style: 'Heading2', bold: true, size: 28 }),
  imagePlaceholder('Hình 2.16. Xem danh sách danh mục'),
  imagePlaceholder('Hình 2.17. Xem chi tiết danh mục và công thức'),
  imagePlaceholder('Hình 2.18. Tạo danh mục mới'),
  imagePlaceholder('Hình 2.19. Tạo công thức mới'),
  imagePlaceholder('Hình 2.20. Danh mục và công thức sau khi tạo'),
  imagePlaceholder('Hình 2.21. Sửa và xóa danh mục'),

  p('2.4. Các màn hình Module FR-RCP', { style: 'Heading2', bold: true, size: 28 }),
  imagePlaceholder('Hình 2.22. Xem chi tiết công thức'),
  imagePlaceholder('Hình 2.23. Tạo Công Thức'),
  imagePlaceholder('Hình 2.24. Trang xem quản lý công thức của người dùng'),

  p('2.5. Các màn hình Module FR-SRCH, FR-FILE, FR-JOB, FR-OBS', { style: 'Heading2', bold: true, size: 28 }),
  imagePlaceholder('Hình 2.25. Tìm kiếm'),
  imagePlaceholder('Hình 2.26. Lọc sắp xếp'),
  imagePlaceholder('Hình 2.27. Các thuộc tính của item để tiện cho sắp xếp'),
  imagePlaceholder('Hình 2.28. Quản lý hình ảnh'),
  imagePlaceholder('Hình 2.29. Quản lý hình ảnh - MinIO bucket / Supabase Storage'),
  imagePlaceholder('Hình 2.30. Quản lý job thành công'),
  imagePlaceholder('Hình 2.31. Job định kỳ (Recurring Jobs)'),
  imagePlaceholder('Hình 2.32. SiteMap'),
  imagePlaceholder('Hình 2.33. Health check'),
  imagePlaceholder('Hình 2.34. Structured log'),
  imagePlaceholder('Hình 2.35. Otel metric (số liệu request)'),
  imagePlaceholder('Hình 2.36. Otel trace (truy dấu request)'),
  pageBreak(),

  // --- CHƯƠNG 3 ---
  p('CHƯƠNG 3: HƯỚNG DẪN CÀI ĐẶT VÀ CHẠY CHƯƠNG TRÌNH', { style: 'Heading1', bold: true, align: 'left', size: 30 }),

  p('3.1. Yêu cầu hệ thống', { style: 'Heading2', bold: true, size: 28 }),
  p('- Git.', { indentLeft: 360 }),
  p('- .NET 10 SDK (chỉ cần khi chạy backend không dùng Docker).', { indentLeft: 360 }),
  p('- Node.js 18+ và pnpm (khuyến nghị; npm cũng dùng được).', { indentLeft: 360 }),
  p('- Docker Desktop (khuyến nghị — chạy trọn bộ backend + PostgreSQL + Redis + Mailpit bằng một lệnh).', { indentLeft: 360 }),
  p('- Tài khoản Google Cloud khi chạy chức năng đăng nhập Google thật: tạo OAuth Client ID/Secret điền vào.', { indentLeft: 360 }),

  p('3.2. Cài đặt và chạy backend bằng Docker Compose', { style: 'Heading2', bold: true, size: 28 }),
  p('- Bước 1: Tải mã nguồn', { bold: true }),
  codeBlock([
    'git clone https://github.com/hiendotforwork/PTUDWNC_Nhom06_CulinaryBlog.git',
    'cd PTUDWNC_Nhom06_CulinaryBlog'
  ]),
  p('Nếu đã có mã nguồn, mở terminal tại thư mục gốc dự án.'),

  p('- Bước 2: Cấu hình môi trường', { bold: true }),
  p('Compose sử dụng giá trị phát triển mặc định trong .env.example. Có thể sao chép để tùy chỉnh mật khẩu PostgreSQL và JWT:'),
  codeBlock(['cp .env.example .env']),
  p('Trên PowerShell:'),
  codeBlock(['Copy-Item .env.example .env']),
  p('Để sử dụng chức năng đăng nhập bằng Google, điền GOOGLE_CLIENT_ID và GOOGLE_CLIENT_SECRET vào .env theo thông tin OAuth 2.0 của ứng dụng. Tính năng này có thể bỏ qua khi chỉ chạy các chức năng khác.'),

  p('- Bước 3: Khởi động backend và các dịch vụ', { bold: true }),
  p('Từ thư mục gốc chạy:'),
  codeBlock(['docker compose -f compose.yml -f compose.storage.yml -f compose.minio.yml up -d --build']),
  p('Lần chạy đầu cần Internet để tải và build image. API tự áp dụng migration; dịch vụ minio-init tạo bucket culinary-blog và bật quyền đọc ảnh. Kiểm tra trạng thái chạy:'),
  codeBlock([
    'docker compose -f compose.yml -f compose.storage.yml -f compose.minio.yml ps',
    'docker compose -f compose.yml -f compose.storage.yml -f compose.minio.yml logs minio-init api'
  ]),
  p('Đợi dịch vụ api chạy ổn định rồi mở:'),
  p('• API: http://localhost:5058', { indentLeft: 360 }),
  p('• Kiểm tra sức khỏe: http://localhost:5058/health/ready', { indentLeft: 360 }),
  p('• Tài liệu API (Development): http://localhost:5058/scalar', { indentLeft: 360 }),
  p('• Hộp thư phát triển Mailpit: http://localhost:8025', { indentLeft: 360 }),
  p('• MinIO S3 API: http://localhost:9000', { indentLeft: 360 }),

  p('- Bước 4: Nhập bộ công thức mẫu', { bold: true }),
  p('Sau khi PostgreSQL và MinIO đã chạy, mở terminal mới tại thư mục gốc repository và chạy lệnh sau để đồng bộ dữ liệu trong recipes.json cùng ảnh trong images/:'),
  codeBlock([
    'ConnectionStrings__DefaultConnection="Host=localhost;Port=55432;Database=culinary_blog;Username=culinary;Password=${POSTGRES_PASSWORD:-culinary_dev_password}" \\',
    'FileStorage__Provider=Minio \\',
    'MinIO__Endpoint=localhost:9000 \\',
    'MinIO__AccessKey="$MINIO_ROOT_USER" \\',
    'MinIO__SecretKey="$MINIO_ROOT_PASSWORD" \\',
    'MinIO__UseSsl=false \\',
    'MinIO__BucketName=culinary-blog \\',
    'MinIO__PublicBaseUrl=http://localhost:9000 \\',
    'dotnet run --project src/CulinaryBlog.API -- --sync-recipes'
  ]),

  p('3.3. Cài đặt và chạy frontend', { style: 'Heading2', bold: true, size: 28 }),
  p('Mở terminal thứ hai chạy:'),
  codeBlock([
    'cd frontend',
    'pnpm install',
    'pnpm dev'
  ]),
  p('Mở http://localhost:3001. Frontend mặc định gọi API tại http://localhost:5058; nếu API chạy ở địa chỉ khác, đặt NEXT_PUBLIC_API_URL trong frontend/.env.local trước khi khởi động lại frontend, ví dụ:'),
  codeBlock(['NEXT_PUBLIC_API_URL=http://localhost:5058']),

  p('3.4. Dừng chương trình và dữ liệu', { style: 'Heading2', bold: true, size: 28 }),
  p('Dừng container, giữ nguyên dữ liệu:'),
  codeBlock(['docker compose -f compose.yml -f compose.storage.yml -f compose.minio.yml down']),
  p('Xóa container cùng dữ liệu PostgreSQL, ảnh và khóa Data Protection lưu trong volume (không thể khôi phục sau khi xóa):'),
  codeBlock(['docker compose -f compose.yml -f compose.storage.yml -f compose.minio.yml down -v']),
  p('Dừng frontend bằng Ctrl+C trong terminal đang chạy pnpm dev.'),

  p('3.5. Khắc phục lỗi thường gặp', { style: 'Heading2', bold: true, size: 28 }),
  p('• Báo thiếu MINIO_ROOT_USER hoặc MINIO_ROOT_PASSWORD: thêm hai biến vào .env tại thư mục gốc, rồi chạy lại lệnh. Kiểm tra terminal đang đứng đúng thư mục repository.', { indentLeft: 360 }),
  p('• minio-init lỗi hoặc API không kết nối được MinIO: xem log bằng docker compose -f compose.yml -f compose.storage.yml -f compose.minio.yml logs minio minio-init api. Kiểm tra thông tin xác thực trong .env, sau đó chạy lại các dịch vụ bằng docker compose -f compose.yml -f compose.storage.yml -f compose.minio.yml up -d minio minio-init api. API cần chờ minio-init tạo bucket xong.', { indentLeft: 360 }),
  p('• Không mở được MinIO Console hoặc S3 API: kiểm tra docker compose -f compose.yml -f compose.storage.yml -f compose.minio.yml ps và log MinIO. Console dùng cổng 9001, S3 API dùng cổng 9000. Dừng tiến trình đang chiếm cổng hoặc đổi ánh xạ trong compose.storage.yml.', { indentLeft: 360 }),
  p('• Ảnh tải lên được nhưng không hiển thị: kiểm tra bucket culinary-blog trong Console, xác nhận minio-init hoàn tất thành công và MinIO__PublicBaseUrl trong compose.minio.yml là URL trình duyệt truy cập được (mặc định http://localhost:9000). Nếu đổi cổng S3, cập nhật URL này tương ứng.', { indentLeft: 360 }),
  p('• API chưa sẵn sàng hoặc migration thất bại: xem docker compose -f compose.yml -f compose.storage.yml -f compose.minio.yml logs postgres api; chờ PostgreSQL khỏe rồi API hoàn tất migration. Kiểm tra cổng máy 55432 chưa bị chiếm và còn đủ dung lượng ổ đĩa cho Docker.', { indentLeft: 360 }),
  p('• Frontend báo lỗi mạng khi gọi API: xác nhận API hoạt động ở cổng 5058 và NEXT_PUBLIC_API_URL trong frontend/.env.local trỏ tới http://localhost:5058. Sau khi sửa file, dừng rồi chạy lại pnpm dev.', { indentLeft: 360 }),
  p('• Không thấy email xác nhận: mở http://localhost:8025. Mailpit chỉ nhận email để kiểm tra trên máy, không gửi thư ra ngoài. Nếu thư không xuất hiện, xem log docker compose -f compose.yml -f compose.storage.yml -f compose.minio.yml logs api mailpit.', { indentLeft: 360 }),
  p('• Cổng bị chiếm: các cổng mặc định là 3001 (frontend), 5058 (API), 55432 (PostgreSQL), 8025 (Mailpit), 9000 (MinIO S3) và 9001 (MinIO Console). Dừng ứng dụng đang sử dụng cổng hoặc chỉnh ánh xạ cổng trong file Compose tương ứng.', { indentLeft: 360 }),
  p('• Muốn cài lại sạch dữ liệu: chạy docker compose -f compose.yml -f compose.storage.yml -f compose.minio.yml down -v. Lệnh sẽ xóa database, ảnh MinIO và khóa Data Protection trong volume. Chỉ dùng khi chấp nhận mất toàn bộ dữ liệu phát triển.', { indentLeft: 360 })
];

const sectPr = `
  <w:sectPr>
    <w:pgSz w:w="11906" w:h="16838"/>
    <w:pgMar w:top="1134" w:right="1134" w:bottom="1134" w:left="1701" w:header="720" w:footer="720" w:gutter="0"/>
    <w:cols w:space="720"/>
    <w:docGrid w:linePitch="360"/>
  </w:sectPr>
`;

const documentXml = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <w:body>
    ${docBody.join('\n')}
    ${sectPr}
  </w:body>
</w:document>`;

fs.writeFileSync(path.join(tempDir, 'word', 'document.xml'), documentXml, 'utf8');

console.log('Document XML written successfully.');
