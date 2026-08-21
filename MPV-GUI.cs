using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

internal sealed class MainForm : Form
{
    private readonly string root;
    private readonly string mpvPath;
    private readonly string configDir;
    private string selectedFile = string.Empty;
    private readonly ComboBox modeBox;
    private readonly TextBox fileBox;
    private readonly Label infoLabel;
    private readonly Label statusLabel;

    public MainForm()
    {
        root = Path.GetDirectoryName(Application.ExecutablePath);
        mpvPath = Path.Combine(root, "mpv.exe");
        configDir = Path.Combine(root, "portable_config");

        Text = "MPV 插帧 + 超分控制台";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(700, 370);
        MinimumSize = new Size(680, 380);
        AllowDrop = true;
        BackColor = Color.WhiteSmoke;

        Label title = new Label();
        title.Text = "MPV 播放模式";
        title.Location = new Point(24, 20);
        title.AutoSize = true;
        title.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);

        modeBox = new ComboBox();
        modeBox.Location = new Point(145, 17);
        modeBox.Size = new Size(390, 30);
        modeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        modeBox.Items.AddRange(new object[]
        {
            "普通播放（无 AI 插帧）",
            "RIFE 2x 插帧",
            "RIFE 2x + Real-ESRGAN 轻量超分",
            "低负载 RIFE 2x",
            "GLSL 轻量增强（不调用 AI）"
        });
        modeBox.SelectedIndex = 0;
        modeBox.SelectedIndexChanged += delegate { UpdateModeInfo(); };

        Button chooseButton = new Button();
        chooseButton.Text = "选择视频...";
        chooseButton.Location = new Point(555, 16);
        chooseButton.Size = new Size(120, 32);
        chooseButton.Click += ChooseFile;

        Label fileLabel = new Label();
        fileLabel.Text = "视频文件";
        fileLabel.Location = new Point(24, 73);
        fileLabel.AutoSize = true;

        fileBox = new TextBox();
        fileBox.Location = new Point(100, 69);
        fileBox.Size = new Size(575, 30);
        fileBox.ReadOnly = true;
        fileBox.AllowDrop = true;
        fileBox.DragEnter += FileDragEnter;
        fileBox.DragDrop += FileDragDrop;

        Label dropHint = new Label();
        dropHint.Text = "也可以直接把视频拖到这里，或拖到整个窗口。";
        dropHint.Location = new Point(100, 102);
        dropHint.AutoSize = true;
        dropHint.ForeColor = Color.DimGray;

        Button playButton = new Button();
        playButton.Text = "开始播放";
        playButton.Location = new Point(24, 140);
        playButton.Size = new Size(140, 40);
        playButton.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        playButton.Click += PlayFile;

        Button clearButton = new Button();
        clearButton.Text = "清除文件";
        clearButton.Location = new Point(180, 140);
        clearButton.Size = new Size(120, 40);
        clearButton.Click += delegate
        {
            selectedFile = string.Empty;
            fileBox.Text = string.Empty;
            statusLabel.Text = "请选择一个视频文件。";
        };

        Button openFolderButton = new Button();
        openFolderButton.Text = "打开整合包目录";
        openFolderButton.Location = new Point(316, 140);
        openFolderButton.Size = new Size(145, 40);
        openFolderButton.Click += delegate
        {
            Process.Start(new ProcessStartInfo("explorer.exe", Quote(root)) { UseShellExecute = true });
        };

        Button checkButton = new Button();
        checkButton.Text = "检查环境";
        checkButton.Location = new Point(477, 140);
        checkButton.Size = new Size(120, 40);
        checkButton.Click += CheckEnvironment;

        GroupBox infoGroup = new GroupBox();
        infoGroup.Text = "当前模式说明";
        infoGroup.Location = new Point(24, 205);
        infoGroup.Size = new Size(651, 105);

        infoLabel = new Label();
        infoLabel.Location = new Point(16, 27);
        infoLabel.Size = new Size(615, 60);
        infoGroup.Controls.Add(infoLabel);

        statusLabel = new Label();
        statusLabel.Text = "请选择一个视频文件。";
        statusLabel.Location = new Point(24, 335);
        statusLabel.Size = new Size(650, 28);
        statusLabel.ForeColor = Color.DarkSlateGray;

        Controls.Add(title);
        Controls.Add(modeBox);
        Controls.Add(chooseButton);
        Controls.Add(fileLabel);
        Controls.Add(fileBox);
        Controls.Add(dropHint);
        Controls.Add(playButton);
        Controls.Add(clearButton);
        Controls.Add(openFolderButton);
        Controls.Add(checkButton);
        Controls.Add(infoGroup);
        Controls.Add(statusLabel);

        DragEnter += FileDragEnter;
        DragDrop += FileDragDrop;
        UpdateModeInfo();
    }

    private void ChooseFile(object sender, EventArgs e)
    {
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Title = "选择要播放的视频";
            dialog.Filter = "视频文件|*.mp4;*.mkv;*.avi;*.mov;*.webm;*.ts;*.m2ts;*.flv;*.wmv;*.m4v;*.mpg;*.mpeg|所有文件|*.*";
            dialog.Multiselect = false;
            if (dialog.ShowDialog(this) == DialogResult.OK)
                SetSelectedFile(dialog.FileName);
        }
    }

    private void SetSelectedFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        selectedFile = path;
        fileBox.Text = path;
        statusLabel.Text = "已选择：" + path;
    }

    private void FileDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effect = DragDropEffects.Copy;
        else
            e.Effect = DragDropEffects.None;
    }

    private void FileDragDrop(object sender, DragEventArgs e)
    {
        string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (paths != null && paths.Length > 0)
            SetSelectedFile(paths[0]);
    }

    private void UpdateModeInfo()
    {
        switch (modeBox.SelectedIndex)
        {
            case 0:
                infoLabel.Text = "普通硬件解码播放。不会加载 RIFE 或 AI 超分，最适合 4K 视频。";
                break;
            case 1:
                infoLabel.Text = "RIFE v4.6 神经网络插帧，通常把原视频帧率提高到 2 倍。适合 720p/1080p。";
                break;
            case 2:
                infoLabel.Text = "Real-ESRGAN 轻量 x2 超分后再使用 RIFE。画质和负载都较高，建议 1080p 或更低。";
                break;
            case 3:
                infoLabel.Text = "RIFE 插帧，同时关闭 MPV 显示插值并使用较轻的缩放算法；仍然会运行 RIFE AI。";
                break;
            case 4:
                infoLabel.Text = "只使用 MPV GLSL 去色带和锐化，不调用 AI，适合 4K 低负载播放。";
                break;
        }
    }

    private void PlayFile(object sender, EventArgs e)
    {
        if (!File.Exists(mpvPath))
        {
            MessageBox.Show(this, "找不到 mpv.exe。", "MPV 整合包", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (string.IsNullOrWhiteSpace(selectedFile) || !File.Exists(selectedFile))
        {
            MessageBox.Show(this, "请先选择一个存在的视频文件。", "MPV 整合包", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        List<string> args = new List<string>();
        args.Add("--config-dir=" + configDir);
        switch (modeBox.SelectedIndex)
        {
            case 1:
                args.Add("--vf-add=vapoursynth=~~/vs/FrameInterpolation/RIFE_LIGHT_4_6.vpy");
                break;
            case 2:
                args.Add("--vf-add=vapoursynth=~~/vs/Upscale/ESRGAN_Light_RealESRGANv2_animevideo_xsx2.vpy");
                args.Add("--vf-add=vapoursynth=~~/vs/FrameInterpolation/RIFE_LIGHT_4_6.vpy");
                break;
            case 3:
                args.Add("--hwdec=auto-copy");
                args.Add("--video-sync=audio");
                args.Add("--interpolation=no");
                args.Add("--scale=bilinear");
                args.Add("--cscale=bilinear");
                args.Add("--dscale=bilinear");
                args.Add("--vf-add=vapoursynth=~~/vs/FrameInterpolation/RIFE_LIGHT_4_6.vpy");
                break;
            case 4:
                args.Add("--hwdec=auto-copy");
                args.Add("--vo=gpu-next");
                args.Add("--scale=lanczos");
                args.Add("--cscale=lanczos");
                args.Add("--dscale=lanczos");
                args.Add("--glsl-shaders=~~/shaders/hdeband.glsl");
                args.Add("--glsl-shaders-append=~~/shaders/adaptive_sharpen_RT.glsl");
                break;
        }
        args.Add(selectedFile);

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = mpvPath;
        startInfo.Arguments = JoinArguments(args);
        startInfo.WorkingDirectory = root;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.EnvironmentVariables["PATH"] = root + ";" + Path.Combine(root, "vs-plugins") + ";" + Path.Combine(root, "vs-plugins", "vsmlrt-cuda") + ";" + Environment.GetEnvironmentVariable("PATH");

        try
        {
            Process.Start(startInfo);
            statusLabel.Text = "已启动 MPV：" + modeBox.Text;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "启动 MPV 失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CheckEnvironment(object sender, EventArgs e)
    {
        List<string> report = new List<string>();
        bool ok = true;
        ok = AddCheck(report, ok, "mpv.exe", mpvPath, false);
        ok = AddCheck(report, ok, "portable_config", configDir, true);
        ok = AddCheck(report, ok, "RIFE 预设", Path.Combine(configDir, "vs", "FrameInterpolation", "RIFE_LIGHT_4_6.vpy"), false);
        ok = AddCheck(report, ok, "Real-ESRGAN 预设", Path.Combine(configDir, "vs", "Upscale", "ESRGAN_Light_RealESRGANv2_animevideo_xsx2.vpy"), false);
        ok = AddCheck(report, ok, "CUDA/TensorRT", Path.Combine(root, "vs-plugins", "vsmlrt-cuda", "trtexec.exe"), false);
        ok = AddCheck(report, ok, "RIFE 模型", Path.Combine(root, "vs-plugins", "models", "rife_v2", "rife_v4.6.onnx"), false);
        ok = AddCheck(report, ok, "Real-ESRGAN 模型", Path.Combine(root, "vs-plugins", "models", "RealESRGANv2-animevideo-xsx2.onnx"), false);
        report.Add("");
        report.Add(ok ? "检查结果：基础组件齐全。" : "检查结果：有组件缺失，请重新解压完整发布包。 ");
        MessageBox.Show(this, string.Join(Environment.NewLine, report), "MPV 整合包环境检查", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private static bool AddCheck(List<string> report, bool current, string label, string path, bool directory)
    {
        bool exists = directory ? Directory.Exists(path) : File.Exists(path);
        report.Add((exists ? "[OK] " : "[缺失] ") + label);
        return current && exists;
    }

    private static string JoinArguments(List<string> args)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < args.Count; i++)
        {
            if (i > 0) builder.Append(' ');
            builder.Append(Quote(args[i]));
        }
        return builder.ToString();
    }

    private static string Quote(string value)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append('"');
        int slashCount = 0;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '\\')
            {
                slashCount++;
                continue;
            }
            if (c == '"')
            {
                builder.Append('\\', slashCount * 2 + 1);
                builder.Append('"');
                slashCount = 0;
                continue;
            }
            builder.Append('\\', slashCount);
            slashCount = 0;
            builder.Append(c);
        }
        builder.Append('\\', slashCount * 2);
        builder.Append('"');
        return builder.ToString();
    }
}

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
