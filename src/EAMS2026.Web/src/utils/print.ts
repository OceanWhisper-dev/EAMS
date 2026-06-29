export interface PrintColumn<T = any> {
  label: string
  value: ((row: T) => string | number) | string
  align?: 'left' | 'center' | 'right'
}

export function openPrintWindow<T>(title: string, columns: PrintColumn<T>[], data: T[], subtitle?: string) {
  const printWindow = window.open('', '_blank')
  if (!printWindow) {
    return
  }

  const headerCells = columns.map(col => {
    const align = col.align === 'center' ? ' text-align: center;' : col.align === 'right' ? ' text-align: right;' : ''
    return `<th style="${align}">${col.label}</th>`
  }).join('')

  const rows = data.map(item => {
    const cells = columns.map(col => {
      const val = typeof col.value === 'function' ? col.value(item) : (item as any)[col.value]
      const align = col.align === 'center' ? ' text-align: center;' : col.align === 'right' ? ' text-align: right;' : ''
      return `<td style="${align}">${val ?? ''}</td>`
    }).join('')
    return `<tr>${cells}</tr>`
  }).join('')

  const printDate = new Date().toLocaleString('zh-CN', {
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', second: '2-digit'
  })

  printWindow.document.write(`<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<title>${title}</title>
<style>
* { margin: 0; padding: 0; box-sizing: border-box; }
body { font-family: 'Microsoft YaHei', 'SimSun', Arial, sans-serif; margin: 30px; color: #333; }
h1 { text-align: center; font-size: 18px; margin-bottom: 4px; padding-bottom: 10px; border-bottom: 2px solid #333; }
.subtitle { text-align: center; font-size: 14px; color: #666; margin-bottom: 20px; }
table { width: 100%; border-collapse: collapse; font-size: 12px; }
th, td { border: 1px solid #999; padding: 6px 8px; }
th { background-color: #e8e8e8; font-weight: bold; }
tr:nth-child(even) { background-color: #f8f8f8; }
.footer { text-align: center; margin-top: 20px; font-size: 10px; color: #999; }
@media print {
  body { margin: 15mm; }
  h1 { font-size: 16pt; }
  table { font-size: 10pt; }
  th, td { padding: 4pt 6pt; }
}
</style>
</head>
<body>
<h1>${title}</h1>
${subtitle ? `<div class="subtitle">${subtitle}</div>` : ''}
<table>
<thead><tr>${headerCells}</tr></thead>
<tbody>${rows}</tbody>
</table>
<div class="footer">打印时间: ${printDate}</div>
<script>
  window.onload = function () {
    window.print();
    window.close();
  }
<\/script>
</body>
</html>`)
  printWindow.document.close()
}

export function flattenTree<T extends { children?: T[] }>(items: T[]): T[] {
  const result: T[] = []
  function walk(list: T[]) {
    for (const item of list) {
      result.push(item)
      if (item.children && item.children.length > 0) {
        walk(item.children)
      }
    }
  }
  walk(items)
  return result
}