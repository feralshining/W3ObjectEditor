using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace W3ObjectEditor
{
    public partial class Form1 : MetroFramework.Forms.MetroForm
    {
        private List<int> _searchMatchIndices = new List<int>();
        private int _currentMatchIndex = -1;
        private string currentFilePath;
        public Form1()
        {
            InitializeComponent();
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.EditMode = DataGridViewEditMode.EditProgrammatically;
            dataGridView1.SelectionChanged += dataGridView1_SelectionChanged;
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            int selected = dataGridView1.SelectedRows.Count;
            int total = dataGridView1.Rows.Count;
            int currentMatchIndex = -1;
            if (selected > 0)
                currentMatchIndex = dataGridView1.SelectedRows[0].Index + 1;

            UpdateStatusBar(total, selected, currentMatchIndex);
        }

        private void UpdateStatusBar(int total, int selected, int currentMatchIndex)
        {
            lblTotalCount.Text = $"총 데이터 수 : {total}";
            lblSelectedCount.Text = $"선택됨 : {selected}";
            lblDataIndex.Text = $"선택한 데이터 인덱스 : {currentMatchIndex}";
        }

        // 간단한 CSV 파서 (쉼표+따옴표 감싼 값 처리)
        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"'); i++; // escaped quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }
        private string EscapeCsv(object value)
        {
            if (value == null || value == DBNull.Value) return "";

            string str = value.ToString();

            // 줄바꿈, 쉼표, 따옴표 있으면 전체 감싸고 내부 따옴표 escape
            if (str.Contains(",") || str.Contains("\"") || str.Contains("\n") || str.Contains("\r"))
            {
                return "\"" + str.Replace("\"", "\"\"") + "\"";
            }

            return str;
        }

        private async void btnOpen_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "W3 Object Files (*.w3u;*.w3a;*.w3h;*.w3t)|*.w3u;*.w3a;*.w3h;*.w3t|All files (*.*)|*.*",
                Title = "W3 오브젝트 파일 열기"
            })
            {
                if (ofd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    pictureBox1.Visible = false;
                    dataGridView1.DataSource = null;

                    var dataTable = await W3ObjectFileHandler.LoadAsync(ofd.FileName);
                    dataGridView1.DataSource = dataTable;

                    if (dataGridView1.Columns.Count > 5)
                        dataGridView1.Columns[5].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

                    string ext = Path.GetExtension(ofd.FileName).ToLower();
                    bool showLevelPointerColumns = (ext == ".w3a");

                    if (dataGridView1.Columns.Contains("Level"))
                        dataGridView1.Columns["Level"].Visible = showLevelPointerColumns;

                    if (dataGridView1.Columns.Contains("DataPointer"))
                        dataGridView1.Columns["DataPointer"].Visible = showLevelPointerColumns;

                    currentFilePath = ofd.FileName;
                    UpdateStatusBar(dataTable.Rows.Count, 0, 0);
                }

                catch (Exception ex)
                {
                    MessageBox.Show($"파일 로드 중 오류 발생:\n{ex.Message}", "알림", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string keyword = metroTextBox1.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(keyword)) return;

            var dt = dataGridView1.DataSource as DataTable;
            if (dt == null || dt.Rows.Count == 0) return;

            _searchMatchIndices.Clear();
            _currentMatchIndex = -1;

            bool firstMatchFocused = false;
            dataGridView1.ClearSelection();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                bool match = false;
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.Value != null && cell.Value.ToString().ToLower().Contains(keyword))
                    {
                        match = true;
                        break;
                    }
                }

                if (match)
                {
                    _searchMatchIndices.Add(row.Index);
                    row.DefaultCellStyle.BackColor = Color.Yellow;

                    if (!firstMatchFocused)
                    {
                        row.Selected = true;
                        dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                        _currentMatchIndex = 0;
                        firstMatchFocused = true;
                    }
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.White;
                    row.Selected = false;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var dt = dataGridView1.DataSource as DataTable;
            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("저장할 데이터가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(currentFilePath))
            {
                MessageBox.Show("현재 파일 경로가 설정되어 있지 않습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                W3ObjectFileHandler.Save(currentFilePath, dt);
                MessageBox.Show("저장 완료되었습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 중 오류 발생:\n{ex.Message}", "알림", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSaveAs_Click(object sender, EventArgs e)
        {
            var dt = dataGridView1.DataSource as DataTable;
            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("저장할 데이터가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "W3 Object Files (*.w3u;*.w3a;*.w3h;*.w3t)|*.w3u;*.w3a;*.w3h;*.w3t",
                Title = "다른 이름으로 저장",
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        W3ObjectFileHandler.Save(sfd.FileName, dt);
                        MessageBox.Show("다른 이름으로 저장 완료되었습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"저장 중 오류 발생:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private void btnImport_Click(object sender, EventArgs e)
        {
            var dt = dataGridView1.DataSource as DataTable;
            if (dt == null)
            {
                dt = W3ObjectFileHandler.CreateEmptyDataTable();
                dataGridView1.DataSource = dt;
                pictureBox1.Visible = false;

                if (dataGridView1.Columns.Contains("Level"))
                    dataGridView1.Columns["Level"].Visible = true;

                if (dataGridView1.Columns.Contains("DataPointer"))
                    dataGridView1.Columns["DataPointer"].Visible = true;
            }

            using (OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "CSV 오브젝트 데이터 불러오기"
            })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        int importCount = 0;

                        using (StreamReader sr = new StreamReader(ofd.FileName, Encoding.UTF8))
                        {
                            sr.ReadLine(); // 헤더 스킵

                            List<string[]> parsedRows = new List<string[]>();
                            StringBuilder currentLine = new StringBuilder();
                            string rawLine;
                            while ((rawLine = sr.ReadLine()) != null)
                            {
                                currentLine.AppendLine(rawLine);
                                string line = currentLine.ToString();

                                int quoteCount = line.Count(c => c == '"');
                                if (quoteCount % 2 == 0)
                                {
                                    string[] parts = ParseCsvLine(line.TrimEnd('\r', '\n'));
                                    if (parts.Length < 6) { currentLine.Clear(); continue; }

                                    if (string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[4]))
                                    {
                                        currentLine.Clear();
                                        continue;
                                    }

                                    var newRow = dt.NewRow();
                                    newRow["Source"] = parts[0];
                                    newRow["OriginalID"] = parts[1];
                                    newRow["NewID"] = parts[2];
                                    newRow["FieldID"] = parts[3];
                                    newRow["Type"] = parts[4];
                                    newRow["Value"] = string.IsNullOrWhiteSpace(parts[5]) ? "" : parts[5];

                                    if (dt.Columns.Contains("Level") && parts.Length > 6 && int.TryParse(parts[6], out int level))
                                        newRow["Level"] = level;
                                    else if (dt.Columns.Contains("Level"))
                                        newRow["Level"] = 0;

                                    if (dt.Columns.Contains("DataPointer") && parts.Length > 7 && int.TryParse(parts[7], out int dataPtr))
                                        newRow["DataPointer"] = dataPtr;
                                    else if (dt.Columns.Contains("DataPointer"))
                                        newRow["DataPointer"] = 0;

                                    dt.Rows.Add(newRow);
                                    importCount++;
                                    currentLine.Clear();
                                }
                            }
                        }

                        dataGridView1.DataSource = dt;
                        MessageBox.Show($"오브젝트 불러오기 완료 ({importCount}개 항목 추가됨)", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"불러오기 오류:\n{ex.Message}", "알림", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private void btnExport_Click(object sender, EventArgs e)
        {
            if (!(dataGridView1.DataSource is DataTable dt) || dt.Rows.Count == 0)
            {
                MessageBox.Show("내보낼 데이터가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedUnitIds = dataGridView1.SelectedCells
                .Cast<DataGridViewCell>()
                .Select(cell => dataGridView1.Rows[cell.RowIndex])
                .Where(row => !row.IsNewRow)
                .Select(row => new
                {
                    OriginalID = row.Cells["OriginalID"].Value?.ToString() ?? "",
                    NewID = row.Cells["NewID"].Value?.ToString() ?? ""
                })
                .Distinct()
                .ToList();

            if (selectedUnitIds.Count == 0)
            {
                MessageBox.Show("데이터를 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "오브젝트 데이터 내보내기",
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        bool includeExtraColumns = dt.Columns.Contains("Level") && dt.Columns.Contains("DataPointer");

                        using (StreamWriter sw = new StreamWriter(sfd.FileName, false, Encoding.UTF8))
                        {
                            var headerCols = new List<string> { "Source", "OriginalID", "NewID", "FieldID", "Type", "Value" };
                            if (includeExtraColumns)
                            {
                                headerCols.Add("Level");
                                headerCols.Add("DataPointer");
                            }

                            sw.WriteLine(string.Join(",", headerCols));

                            foreach (var id in selectedUnitIds)
                            {
                                var rows = dt.AsEnumerable()
                                    .Where(r =>
                                        r.Field<string>("OriginalID") == id.OriginalID &&
                                        r.Field<string>("NewID") == id.NewID)
                                    .ToList();

                                if (rows.Count == 0)
                                {
                                    var nullRow = new List<string>
                                    {
                                        EscapeCsv("Unknown"),
                                        EscapeCsv(id.OriginalID),
                                        EscapeCsv(id.NewID),
                                        "", "", ""
                                    };
                                    if (includeExtraColumns)
                                    {
                                        nullRow.Add("");
                                        nullRow.Add("");
                                    }
                                    sw.WriteLine(string.Join(",", nullRow));
                                }
                                else
                                {
                                    foreach (var r in rows)
                                    {
                                        var fields = new List<string>
                                        {
                                            EscapeCsv(r["Source"]),
                                            EscapeCsv(r["OriginalID"]),
                                            EscapeCsv(r["NewID"]),
                                            EscapeCsv(r["FieldID"]),
                                            EscapeCsv(r["Type"]),
                                            EscapeCsv(r["Value"].ToString().Replace("\n", "\\n")) // 핵심
                                        };

                                        if (includeExtraColumns)
                                        {
                                            fields.Add(EscapeCsv(r["Level"]));
                                            fields.Add(EscapeCsv(r["DataPointer"]));
                                        }

                                        sw.WriteLine(string.Join(",", fields));
                                    }
                                }
                            }
                        }

                        MessageBox.Show("내보내기 완료", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"내보내기 오류:\n{ex.Message}", "알림", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            if (_searchMatchIndices.Count == 0)
            {
                MessageBox.Show("검색 결과가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _currentMatchIndex = (_currentMatchIndex - 1 + _searchMatchIndices.Count) % _searchMatchIndices.Count;
            int rowIndex = _searchMatchIndices[_currentMatchIndex];

            dataGridView1.ClearSelection();
            dataGridView1.Rows[rowIndex].Selected = true;
            dataGridView1.FirstDisplayedScrollingRowIndex = rowIndex;
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (_searchMatchIndices.Count == 0)
            {
                MessageBox.Show("검색 결과가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _currentMatchIndex = (_currentMatchIndex + 1) % _searchMatchIndices.Count;
            int rowIndex = _searchMatchIndices[_currentMatchIndex];

            dataGridView1.ClearSelection();
            dataGridView1.Rows[rowIndex].Selected = true;
            dataGridView1.FirstDisplayedScrollingRowIndex = rowIndex;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            _searchMatchIndices.Clear();
            _currentMatchIndex = -1;

            dataGridView1.CancelEdit();
            dataGridView1.DataSource = null;
            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();

            pictureBox1.Visible = true;
            metroTextBox1.Text = string.Empty;

            UpdateStatusBar(0, 0, 0);
        }
        private void editModeToggle_CheckStateChanged(object sender, EventArgs e)
        {
            if (editModeToggle.Checked)
            {
                // 편집 허용 (F2, 더블클릭, 타이핑 등)
                dataGridView1.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;     
            }
            else
            {
                // 편집 금지 (프로그래밍으로만 수정 가능)
                dataGridView1.EditMode = DataGridViewEditMode.EditProgrammatically;
                dataGridView1.EndEdit(); // 혹시 모를 편집 모드 종료
            }
            editModeToggle.Text = editModeToggle.Checked ? "편집 모드: ON" : "편집 모드: OFF";
        }
    }
}
