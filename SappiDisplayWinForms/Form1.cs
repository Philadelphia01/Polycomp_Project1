using System.Globalization;
using System.Text;

namespace SappiDisplayWinForms;

public partial class Form1 : Form
{
    private readonly List<LedInput> _ledInputs = new();
    private readonly List<RowBinding> _rows = new();

    private RichTextBox? _hexOutput;
    private bool _sanitizing;

    public Form1()
    {
        InitializeComponent();
        BuildUi();
        RecomputeAll();
    }

    // Project requirement protocol: each LED field is prefixed by a byte marker for its user-selected colour.
    // We keep these as ASCII "FA/FB/FC" tokens, then hex-encode the full ASCII stream for display.
    private enum LedColor
    {
        Red,
        Green,
        Yellow,
    }

    private enum NumericMode
    {
        Integer,
        Decimal2,
    }

    private sealed record LedInput(TextBox TextBox, ComboBox ColorBox, NumericMode Mode);

    private sealed record RowBinding(
        LedInput? Target,
        LedInput? Actual,
        StatusLight Light,
        decimal? TargetValue,
        decimal? ActualValue
    );

    private sealed class StatusLight : Control
    {
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color FillColor { get; set; } = Color.FromArgb(40, 40, 40);

        public StatusLight()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            Size = new Size(18, 18);
            MinimumSize = new Size(18, 18);
            MaximumSize = new Size(18, 18);
            Margin = new Padding(6, 0, 6, 0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var rect = ClientRectangle;
            rect.Inflate(-2, -2);

            using var brush = new SolidBrush(FillColor);
            using var pen = new Pen(Color.FromArgb(20, 20, 20), 1.5f);
            g.FillEllipse(brush, rect);
            g.DrawEllipse(pen, rect);
        }
    }

    private sealed record DisplayRow(
        string Label,
        string Unit,
        bool HasTarget,
        bool HasActual,
        bool HasStatusLight,
        NumericMode TargetMode,
        NumericMode ActualMode
    );

    private void BuildUi()
    {
        BackColor = Color.FromArgb(10, 50, 110);

        // Layout choice: a SplitContainer makes the bottom output panel resizable during demos/recording.
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            BackColor = Color.FromArgb(10, 50, 110),
            ColumnCount = 1,
            RowCount = 2,
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var title = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 54,
            Text = "PAPER - SAFE PRODUCTION",
            ForeColor = Color.FromArgb(230, 230, 230),
            Font = new Font(Font.FontFamily, 20, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        root.Controls.Add(title, 0, 0);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            BackColor = Color.FromArgb(10, 50, 110),
            BorderStyle = BorderStyle.None,
            FixedPanel = FixedPanel.None,
            IsSplitterFixed = false,
            SplitterWidth = 8,
            Panel1MinSize = 220,
            Panel2MinSize = 110,
        };
        root.Controls.Add(split, 0, 1);

        var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, AutoScroll = true };
        split.Panel1.Controls.Add(content);

        var bottom = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(5, 30, 70), Padding = new Padding(10), BorderStyle = BorderStyle.FixedSingle };
        split.Panel2.Controls.Add(bottom);

        var hexLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 30,
            Text = "Combined output (hex) - updates on every change",
            ForeColor = Color.FromArgb(220, 220, 220),
            Font = new Font(Font.FontFamily, 11, FontStyle.Bold),
        };
        bottom.Controls.Add(hexLabel);

        _hexOutput = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(15, 15, 15),
            ForeColor = Color.FromArgb(110, 255, 110),
            Font = new Font(Font.FontFamily, 11, FontStyle.Bold),
            DetectUrls = false,
            WordWrap = true,
            Text = "HEX: (waiting for input...)",
        };
        bottom.Controls.Add(_hexOutput);

        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 1,
        };
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        content.Controls.Add(outer);

        outer.Controls.Add(BuildSectionPanel("PM1", BuildPmRows()), 0, 0);
        outer.Controls.Add(BuildSectionPanel("PM2", BuildPmRows()), 1, 0);

        // Default split: keep output large and obvious, but user can drag.
        split.SplitterDistance = Math.Max(300, Height - 320);
    }

    private Panel BuildSectionPanel(string sectionTitle, List<DisplayRow> rows)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(12, 60, 125),
            Padding = new Padding(12),
            Margin = new Padding(6),
        };

        var header = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = sectionTitle,
            ForeColor = Color.FromArgb(230, 230, 230),
            Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
        };
        panel.Controls.Add(header);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 5,
            AutoSize = false,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38)); // label
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24)); // target
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24)); // actual
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56)); // unit
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28)); // light

        var colHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 26,
            BackColor = Color.Transparent,
            ColumnCount = 5,
        };
        colHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        colHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        colHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        colHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        colHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28));

        colHeader.Controls.Add(MakeHeaderCell(""), 0, 0);
        colHeader.Controls.Add(MakeHeaderCell("TARGET"), 1, 0);
        colHeader.Controls.Add(MakeHeaderCell("ACTUAL"), 2, 0);
        colHeader.Controls.Add(MakeHeaderCell(""), 3, 0);
        colHeader.Controls.Add(MakeHeaderCell(""), 4, 0);

        panel.Controls.Add(colHeader);
        panel.Controls.Add(grid);
        grid.BringToFront();

        int r = 0;
        foreach (var row in rows)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            grid.RowCount = r + 1;

            var label = new Label
            {
                Dock = DockStyle.Fill,
                Text = row.Label,
                ForeColor = Color.FromArgb(230, 230, 230),
                Font = new Font(Font.FontFamily, 10.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
            };
            grid.Controls.Add(label, 0, r);

            LedInput? target = row.HasTarget ? MakeLedInput(row.TargetMode) : null;
            LedInput? actual = row.HasActual ? MakeLedInput(row.ActualMode) : null;

            if (target is not null) grid.Controls.Add(WrapLed(target), 1, r);
            else grid.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }, 1, r);

            if (actual is not null) grid.Controls.Add(WrapLed(actual), 2, r);
            else grid.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }, 2, r);

            var unit = new Label
            {
                Dock = DockStyle.Fill,
                Text = row.Unit,
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
            };
            grid.Controls.Add(unit, 3, r);

            var light = new StatusLight { Visible = row.HasStatusLight };
            var lightHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            lightHost.Controls.Add(light);
            light.Location = new Point((lightHost.Width - light.Width) / 2, (lightHost.Height - light.Height) / 2);
            lightHost.Resize += (_, _) =>
            {
                light.Left = (lightHost.Width - light.Width) / 2;
                light.Top = (lightHost.Height - light.Height) / 2;
            };
            grid.Controls.Add(lightHost, 4, r);

            _rows.Add(new RowBinding(target, actual, light, null, null));
            r++;
        }

        return panel;
    }

    private static Label MakeHeaderCell(string text) =>
        new()
        {
            Dock = DockStyle.Fill,
            Text = text,
            ForeColor = Color.FromArgb(215, 215, 215),
            Font = new Font(SystemFonts.DefaultFont.FontFamily, 9.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
        };

    private List<DisplayRow> BuildPmRows() =>
        new()
        {
            new DisplayRow("NET PRODUCTION", "TONS", true, true, true, NumericMode.Decimal2, NumericMode.Decimal2),
            new DisplayRow("DOWNTIME", "%", true, true, true, NumericMode.Decimal2, NumericMode.Decimal2),
            new DisplayRow("SHRINKAGE", "%", true, true, true, NumericMode.Decimal2, NumericMode.Decimal2),
            new DisplayRow("TONS TO MAKE BUDGET", "TONS", true, true, true, NumericMode.Integer, NumericMode.Integer),
            new DisplayRow("SAFETY", "", false, false, false, NumericMode.Integer, NumericMode.Integer),
            new DisplayRow("INJURY FREE DAYS", "DAYS", false, true, false, NumericMode.Integer, NumericMode.Integer),
            new DisplayRow("EFFLUENT", "M/DAY", true, true, true, NumericMode.Decimal2, NumericMode.Decimal2),
        };

    private LedInput MakeLedInput(NumericMode mode)
    {
        var tb = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = HorizontalAlignment.Left,
            BackColor = Color.FromArgb(20, 20, 20),
            ForeColor = Color.FromArgb(235, 160, 235), // default "purple LED"
            Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
            Margin = new Padding(0),
        };

        tb.KeyPress += (_, e) => EnforceNumericKeystroke(mode, tb, e);
        tb.TextChanged += (_, _) =>
        {
            // We validate both typing (KeyPress) and paste/edit scenarios (TextChanged sanitation).
            // This keeps input always conforming to: integer OR decimal with max 2 digits after '.'.
            SanitizeNumericText(tb, mode);
            RecomputeAll();
        };

        var cb = new ComboBox
        {
            Dock = DockStyle.Right,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 64,
            Font = new Font(Font.FontFamily, 9.5f, FontStyle.Regular),
        };
        cb.Items.AddRange(new object[] { "Red", "Green", "Yellow" });
        cb.SelectedIndex = 0;
        cb.SelectedIndexChanged += (_, _) =>
        {
            ApplyLedColor(tb, (LedColor)cb.SelectedIndex);
            RecomputeAll();
        };
        ApplyLedColor(tb, (LedColor)cb.SelectedIndex);

        var led = new LedInput(tb, cb, mode);
        _ledInputs.Add(led);
        return led;
    }

    private static Control WrapLed(LedInput led)
    {
        var host = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0) };
        host.Controls.Add(led.TextBox);
        host.Controls.Add(led.ColorBox);
        return host;
    }

    private static void ApplyLedColor(TextBox tb, LedColor color)
    {
        tb.ForeColor = color switch
        {
            LedColor.Red => Color.FromArgb(255, 80, 80),
            LedColor.Green => Color.FromArgb(110, 255, 110),
            LedColor.Yellow => Color.FromArgb(255, 235, 110),
            _ => tb.ForeColor
        };
    }

    private static void EnforceNumericKeystroke(NumericMode mode, TextBox tb, KeyPressEventArgs e)
    {
        if (char.IsControl(e.KeyChar))
            return;

        if (mode == NumericMode.Integer)
        {
            if (!char.IsDigit(e.KeyChar))
                e.Handled = true;
            return;
        }

        // Decimal2
        if (char.IsDigit(e.KeyChar))
        {
            var text = tb.Text;
            var selStart = tb.SelectionStart;
            var selLen = tb.SelectionLength;
            var next = text.Remove(selStart, selLen).Insert(selStart, e.KeyChar.ToString());

            var dot = next.IndexOf('.');
            if (dot >= 0)
            {
                var decimals = next.Length - dot - 1;
                if (decimals > 2)
                    e.Handled = true;
            }
            return;
        }

        if (e.KeyChar == '.')
        {
            if (tb.Text.Contains('.'))
                e.Handled = true;
            return;
        }

        e.Handled = true;
    }

    private void SanitizeNumericText(TextBox tb, NumericMode mode)
    {
        if (_sanitizing)
            return;

        var original = tb.Text;
        if (string.IsNullOrEmpty(original))
            return;

        var sb = new StringBuilder(original.Length);
        bool seenDot = false;
        int decimals = 0;

        foreach (var ch in original)
        {
            if (char.IsDigit(ch))
            {
                if (mode == NumericMode.Decimal2 && seenDot)
                {
                    if (decimals >= 2)
                        continue;
                    decimals++;
                }
                sb.Append(ch);
                continue;
            }

            if (mode == NumericMode.Decimal2 && ch == '.' && !seenDot)
            {
                seenDot = true;
                sb.Append(ch);
            }
        }

        var sanitized = sb.ToString();
        if (sanitized == original)
            return;

        _sanitizing = true;
        try
        {
            var caret = tb.SelectionStart;
            tb.Text = sanitized;
            tb.SelectionStart = Math.Min(caret, tb.Text.Length);
        }
        finally
        {
            _sanitizing = false;
        }
    }

    private void RecomputeAll()
    {
        // 1) Update row numeric values + status lights
        foreach (var idx in Enumerable.Range(0, _rows.Count))
        {
            var rb = _rows[idx];
            decimal? target = TryParseDecimal(rb.Target?.TextBox.Text);
            decimal? actual = TryParseDecimal(rb.Actual?.TextBox.Text);

            _rows[idx] = rb with { TargetValue = target, ActualValue = actual };
            UpdateStatusLight(rb.Light, target, actual);
        }

        // 2) Build combined stream in the required physical order:
        // left-to-right within a row, then top-to-bottom across the whole screen.
        // (The list is built in control creation order, so iterating _ledInputs preserves that order.)
        var combined = new StringBuilder();
        foreach (var led in _ledInputs)
        {
            combined.Append(ColorPrefix((LedColor)led.ColorBox.SelectedIndex));
            combined.Append(led.TextBox.Text);
        }

        var bytes = Encoding.ASCII.GetBytes(combined.ToString());
        var hex = BitConverter.ToString(bytes).Replace("-", " ");
        if (_hexOutput is not null)
            _hexOutput.Text = string.IsNullOrWhiteSpace(hex) ? "HEX: (no input yet)" : $"HEX: {hex}";
    }

    private static string ColorPrefix(LedColor color) =>
        color switch
        {
            LedColor.Red => "FA",
            LedColor.Green => "FB",
            LedColor.Yellow => "FC",
            _ => "FA"
        };

    private static decimal? TryParseDecimal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var v))
            return v;

        // allow local comma/format if user typed it anyway
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out v))
            return v;

        return null;
    }

    private static void UpdateStatusLight(StatusLight light, decimal? target, decimal? actual)
    {
        if (!light.Visible)
            return;

        if (target is null || actual is null || target <= 0)
        {
            light.FillColor = Color.FromArgb(40, 40, 40);
            light.Invalidate();
            return;
        }

        var green = Color.FromArgb(80, 255, 120);
        var yellow = Color.FromArgb(255, 235, 90);
        var red = Color.FromArgb(255, 90, 90);

        // Threshold rule from requirements:
        // - Green when actual > target
        // - Yellow when target > actual > 0.8*target
        // - Red when actual < 0.8*target
        if (actual > target)
            light.FillColor = green;
        else if (actual > (0.8m * target))
            light.FillColor = yellow;
        else
            light.FillColor = red;

        light.Invalidate();
    }
}
