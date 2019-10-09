using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

/// <summary>
/// auto click matched image.
/// </summary>
public class AutoClickerSub
{
	/// <summary>
	/// template image
	/// </summary>
	public class TemplateFile
	{
		public string FileName { set; get; }
		public string FullName { set; get; }
		public BitmapImage BitmapImage { set; get; }
		public OpenCvSharp.Mat MatImage { set; get; }
		public OpenCvSharp.Mat MatGrayImage { set; get; }
		public int Number { set; get; }
		private int _count;
		public int Count
		{
			get { return _count; }
			set
			{
				_count = value;
			}
		}
		public string sCount
		{
			get { return Count.ToString(); }
			set { Count = int.Parse(value); }
		}
	}

	public class args
	{
		public IntPtr hWnd { get; set; }
		public bool isRealSize { get; set; }
		public bool useHDC { get; set; }
		public bool useColor { get; set; }
		public double dX { get; set; }
		public double dY { get; set; }
		public double threshold { get; set; }

		public args()
		{
			this.hWnd = IntPtr.Zero;
			this.isRealSize = false;
			this.useHDC = false;
			this.useColor = false;
			this.dX = 0.5;
			this.dY = 0.5;
			this.threshold = 0.8;
		}
		public args(IntPtr _hWnd, bool _isrealsize = default, bool _usehdc = default, bool _usecolor = default, double _dX = 1, double _dY = 1, double _threshold = 0.8)
		{
			this.hWnd = _hWnd;
			this.isRealSize = _isrealsize;
			this.useHDC = _usehdc;
			this.useColor = _usecolor;
			this.dX = _dX;
			this.dY = _dY;
			this.threshold = _threshold;
		}
	}

	/// <summary>
	/// store target application window info.
	/// </summary>
	public class WindowInfo
	{
		public IntPtr hWnd { get; set; }
		public string ClassName { get; set; }
		public string Text { get; set; }
		public string ProcessName { get; set; }
		public string FullPathName { get; set; }
		public RECT Rect { get; set; }

		public WindowInfo()
		{
			hWnd = IntPtr.Zero;
			ClassName = "";
			Text = "";
			ProcessName = "";
			FullPathName = "";
			Rect = new RECT();
		}

		public WindowInfo(IntPtr hwnd = default, string className = default, string text = default, string processname = default, string fullpathname = default, RECT rect = default)
		{
			this.hWnd = hwnd;
			this.ClassName = className;
			this.Text = text;
			this.ProcessName = processname;
			this.FullPathName = fullpathname;
			this.Rect = rect;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="hWnd"></param>
	/// <returns></returns>
	public static WindowInfo GetWindowInfo(IntPtr hWnd)
	{
		var className = GetWindowClassName(hWnd);
		var text = GetWindowText(hWnd);
		var rect = GetWindowRectangle(hWnd);
		uint ProcessID = 0;
		uint targetThreadId = NativeMethods.GetWindowThreadProcessId(hWnd, out ProcessID);
		Process ps = Process.GetProcessById((int)ProcessID);
		return new WindowInfo(hWnd, className, text, ps.ProcessName.ToString(), ps.MainModule.FileName, rect);
	}

	/// <summary>
	/// move window to rect.
	/// </summary>
	/// <param name="_hWnd"></param>
	/// <param name="rect"></param>
	public static void SetWindowPos(IntPtr _hWnd, RECT rect)
	{
		NativeMethods.ShowWindow(_hWnd, nCmdShow.SW_RESTORE);
		NativeMethods.SetWindowPos(_hWnd, (IntPtr)hWndInsertAfter.HWND_TOPMOST, rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top, SetWindowPosFlags.AsynchronousWindowPosition | SetWindowPosFlags.ShowWindow);

		// target app to nottopmost
		NativeMethods.SetWindowPos(_hWnd, (IntPtr)hWndInsertAfter.HWND_NOTOPMOST, rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top, SetWindowPosFlags.AsynchronousWindowPosition | SetWindowPosFlags.ShowWindow);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="hWnd"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GetWindowClassName(IntPtr hWnd)
	{
		StringBuilder buffer = new StringBuilder(128);
		NativeMethods.GetClassName(hWnd, buffer, buffer.Capacity);
		return buffer.ToString();
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="hWnd"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GetWindowText(IntPtr hWnd)
	{
		var length = NativeMethods.SendMessage(hWnd, WindowMessage.WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
		var buffer = new StringBuilder(length);
		NativeMethods.SendMessage(hWnd, WindowMessage.WM_GETTEXT, new IntPtr(length + 1), buffer);
		return buffer.ToString();
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="hWnd"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static RECT GetWindowRectangle(IntPtr hWnd)
	{
		RECT rect = new RECT();
		NativeMethods.GetWindowRect(hWnd, ref rect);
		return rect;
	}

	/// <summary>
	/// set AutoClickerSub paramaters
	/// </summary>
	/// <param name="_hWnd">target application window handle/</param>
	/// <param name="_isrealsize">image dont resized. same as _dX,_dY =1.</param>
	/// <param name="_usehdc">get capture with HDC. default is false;</param>
	/// <param name="_usecolor">compare with original color. default is false, compare gray mode.</param>
	/// <param name="_dX">image width resized magnification.</param>
	/// <param name="_dY">image height resized magnification.</param>
	/// <param name="_threshold">compare threshold. default is 0.8</param>
	/// <returns>return args class</returns>
	public static args SetArgs(IntPtr _hWnd, bool _isrealsize = default, bool _usehdc = default, bool _usecolor = default, double _dX = 0.5, double _dY = 0.5, double _threshold = 0.8)
	{
		args args = new args(_hWnd, _isrealsize, _usehdc, _usecolor, _dX, _dY, _threshold);
		return args;
	}

	/// <summary>
	/// get target window handle by title
	/// </summary>
	/// <param name="_title"></param>
	/// <returns></returns>
	public static IntPtr GetWindowHandle(string _title)
	{
		if (string.IsNullOrEmpty(_title)) return IntPtr.Zero;
		return NativeMethods.FindWindow(null, _title);
		/*
		Process[] p = System.Diagnostics.Process.GetProcessesByName(_title);
		foreach (System.Diagnostics.Process ps in p)
		{
			if ((ps.MainWindowHandle != IntPtr.Zero) && (ps.MainWindowHandle != null))
			{
				return ps.MainWindowHandle;
			}
		}
		return IntPtr.Zero;
		*/
	}

	/// <summary>
	/// check image in target application window. image set by filename.
	/// </summary>
	/// <param name="args"></param>
	/// <param name="templatefilename"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (bool, OpenCvSharp.Point?, OpenCvSharp.Point?) CheckImage(args args = default, String templatefilename = default)
	{
		if ((args == default) || (templatefilename == default)) return (false, null, null);
		(OpenCvSharp.Mat captureimage, POINT _) = GetCapture(args);
		(bool retcode, OpenCvSharp.Point? tgtPoint, OpenCvSharp.Point? clickPoint) = CheckImage(args, captureimage, templatefilename);
		return (retcode, tgtPoint, clickPoint);
	}

	/// <summary>
	/// make templatefile from 'filename'
	/// </summary>
	/// <param name="filename"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static TemplateFile MakeTemplateFile(string filename = default)
	{
		if (filename == default) return null;

		FileInfo fileinfo = new FileInfo(filename);
		TemplateFile templatefile = new TemplateFile
		{
			FileName = fileinfo.Name.ToString(),
			FullName = fileinfo.FullName.ToString()
		};

		// convert bitmap to OpenCvSharp.Mat. color & gray.
		templatefile.BitmapImage = new BitmapImage();
		templatefile.BitmapImage.BeginInit();
		templatefile.BitmapImage.CacheOption = BitmapCacheOption.OnLoad;
		templatefile.BitmapImage.CreateOptions = BitmapCreateOptions.None;
		FileStream stream = File.OpenRead(templatefile.FullName);
		templatefile.BitmapImage.StreamSource = stream;
		templatefile.BitmapImage.EndInit();
		templatefile.BitmapImage.Freeze();
		stream.Close();
		templatefile.MatImage = new Mat();
		templatefile.MatGrayImage = new Mat();
		templatefile.MatImage = templatefile.BitmapImage.ToMat();
		Cv2.CvtColor(templatefile.MatImage, templatefile.MatGrayImage, ColorConversionCodes.BGR2GRAY);

		return templatefile;
	}

	/// <summary>
	/// get template image file's in _path
	///     image file is extention in system decode.(ImageCodecInfo)
	/// </summary>
	/// <param name="templateFilePath"></param>
	/// <returns></returns>
	public static Collection<TemplateFile> GetTemplateFileList(string templateFilePath)
	{
		if (string.IsNullOrEmpty(templateFilePath)) { return null; }

		string[] IMAGE_SEARCH_PATTERN_ALL = ImageCodecInfo.GetImageDecoders().Select(ici => ici.FilenameExtension.Split(';')).Aggregate((current, next) => current.Concat(next).ToArray()); // imagefile extention
		List<string> ImgFileList = new List<string>();
		Collection<TemplateFile> templatefiles = new Collection<TemplateFile>();
		templatefiles.Clear();
		try
		{
			foreach (string imgext in IMAGE_SEARCH_PATTERN_ALL)
			{
				ImgFileList.AddRange(Directory.GetFiles(templateFilePath, imgext));
			}
			ImgFileList.Sort();

			int i = 0;
			foreach (string templatefilename in ImgFileList) // load templateimage in memory
			{
				TemplateFile templateFile = MakeTemplateFile(templatefilename);
				templateFile.Number = i;
				templatefiles.Add(templateFile);
				i++;
			}
			return templatefiles;
		}
		catch (Exception e)
		{
			if ((e is FileNotFoundException) || (e is DirectoryNotFoundException))
			{
				//MessageBox.Show(e.Message, "GetTemplateFileList", MessageBoxButton.OK, MessageBoxImage.Warning);
				templatefiles.Clear(); // nothing found
				return null;
			}
			throw e;
		}
	}

	/// <summary>
	/// check image in target application window. image set by user.
	/// </summary>
	/// <param name="args"></param>
	/// <param name="captureimage"></param>
	/// <param name="templatefilename"></param>
	/// <returns></returns>
	private static (bool, OpenCvSharp.Point?, OpenCvSharp.Point?) CheckImage(args args = default, OpenCvSharp.Mat captureimage = default, String templatefilename = default)
	{
		if ((args == default) || (captureimage == default) || (templatefilename == default)) return (false, null, null);

		TemplateFile templatefile = MakeTemplateFile(templatefilename);

		bool ret;
		bool retcode = false;

		OpenCvSharp.Point? tgtPoint = new OpenCvSharp.Point();
		OpenCvSharp.Point clickPoint = new OpenCvSharp.Point();
		Mat matcaptureimage = new Mat();
		Mat TemplateImage = new Mat();

		if (args.useHDC) captureimage.CopyTo(matcaptureimage);
		else Cv2.CvtColor(captureimage, matcaptureimage, ColorConversionCodes.BGR2GRAY);


		if (args.useHDC) templatefile.MatImage.CopyTo(TemplateImage);
		else templatefile.MatGrayImage.CopyTo(TemplateImage);

		(ret, tgtPoint) = MatchTemplate(matcaptureimage, TemplateImage, args.threshold);
		if (ret)
		{
			retcode = true;
			clickPoint = MakeClickPoint(args, templatefile, tgtPoint);
		}
		return (retcode, tgtPoint, clickPoint);
	}

	/// <summary>
	/// check image in templatefiles.
	/// </summary>
	/// <param name="args"></param>
	/// <param name="templatefiles"></param>
	/// <returns></returns>
	public static (bool, OpenCvSharp.Point?, OpenCvSharp.Point?, OpenCvSharp.Mat, string, int?) TemplateMatch(args args = default, Collection<TemplateFile> templatefiles = default)
	{
		if (args == default || templatefiles == default) return (false, null, null, null, null, null);

		(OpenCvSharp.Mat captureimage, POINT _) = GetCapture(args);

		bool ret;
		bool retcode = false; ;
		string MatchedTemplateFileName = null;
		int? number = null;

		OpenCvSharp.Point? tgtPoint = new OpenCvSharp.Point();
		OpenCvSharp.Point clickPoint = new OpenCvSharp.Point();
		OpenCvSharp.Mat matMatchedTemplateImage = new OpenCvSharp.Mat();
		OpenCvSharp.Mat matCaptureImage = new OpenCvSharp.Mat();

		if (args.useColor) captureimage.CopyTo(matCaptureImage);
		else Cv2.CvtColor(captureimage, matCaptureImage, ColorConversionCodes.BGR2GRAY);

		foreach (TemplateFile templatefile in templatefiles)
		{
			OpenCvSharp.Mat TemplateImage = new Mat();

			if (args.useColor) templatefile.MatImage.CopyTo(TemplateImage);
			else templatefile.MatGrayImage.CopyTo(TemplateImage);

			(ret, tgtPoint) = MatchTemplate(matCaptureImage, TemplateImage, args.threshold);
			if (ret)
			{
				retcode = true;
				TemplateImage.CopyTo(matMatchedTemplateImage);
				MatchedTemplateFileName = templatefile.FileName.ToString();
				number = templatefile.Number;
				clickPoint = MakeClickPoint(args, templatefile, tgtPoint);

				break;
			}
		}
		return (retcode, tgtPoint, clickPoint, matMatchedTemplateImage, MatchedTemplateFileName, number);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static OpenCvSharp.Point MakeClickPoint(args args, TemplateFile templatefile, OpenCvSharp.Point? tgtPoint)
	{
		OpenCvSharp.Point clickPoint = new OpenCvSharp.Point(0, 0);
		string _spX = "000";
		string _spY = "000";
		System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(@"[XY]+[-]?\d{3}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
		System.Text.RegularExpressions.Match m = r.Match(templatefile.FileName.ToString());
		while (m.Success)
		{
			var s = m.Value;
			switch (s.Substring(0, 1))
			{
				case "X":
					_spX = s.Substring(1, m.Length - 1);
					break;

				case "Y":
					_spY = s.Substring(1, m.Length - 1);
					break;
			}
			m = m.NextMatch();
		}
		int _pX = int.Parse(_spX);
		int _pY = int.Parse(_spY);
		clickPoint.X = (int)((tgtPoint?.X + (templatefile.MatGrayImage.Width / 2.0) + _pX) / args.dX);
		clickPoint.Y = (int)((tgtPoint?.Y + (templatefile.MatGrayImage.Height / 2.0) + _pY) / args.dY);
		return clickPoint;
	}
	/// <summary>
	/// check matTemplate in matTarget.
	/// </summary>
	/// <param name="matTarget"></param>
	/// <param name="matTemplate"></param>
	/// <param name="threshold"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static (bool ret, OpenCvSharp.Point? maxPoint) MatchTemplate(Mat matTarget = default, Mat matTemplate = default, double threshold = 0.8)
	{
		if (matTarget == default || matTemplate == default) return (false, null);
		if (matTarget.Width < matTemplate.Width || matTarget.Height < matTemplate.Height) return (false, null);

		bool ret = false; // set true found
		OpenCvSharp.Point minPoint = new OpenCvSharp.Point();
		OpenCvSharp.Point maxPoint = new OpenCvSharp.Point();
		Mat result;

		using (result = new Mat(matTarget.Height - matTemplate.Height + 1, matTarget.Width - matTemplate.Width + 1, MatType.CV_8UC1))
		{
			Cv2.MatchTemplate(matTarget, matTemplate, result, TemplateMatchModes.CCoeffNormed);
			Cv2.Threshold(result, result, threshold, 1.0, ThresholdTypes.Binary);
			Cv2.MinMaxLoc(result, out minPoint, out maxPoint);
			Cv2.MinMaxLoc(result, out double minval, out double maxval, out minPoint, out maxPoint);
			if (maxval >= threshold)
			{
				ret = true;
			}

		};
		return (ret, maxPoint);
	}

	/// <summary>
	/// Get Target Windows's Capture use hdc or not;
	///  return original captureimage,resized capture image, ressize by X=dX, resizie by Y=dY
	/// </summary>
	/// <param name="hWnd"></param>
	/// <param name="useHdc"></param>
	/// <returns></returns>
	public static (OpenCvSharp.Mat, POINT) GetCapture(args args)
	{
		OpenCvSharp.Mat MatCapturedImage = null;
		Bitmap BitmapCapturedImage = null;
		Mat MatResizedImage = new Mat();
		POINT capturedsize = new POINT(0, 0);

		using (BitmapCapturedImage = args.useHDC ? GetWindowCaptureHdc(args.hWnd) : GetWindowCapture(args.hWnd))
		{
			if (BitmapCapturedImage == null)
			{
				return (null, capturedsize);
			}
			MatCapturedImage = OpenCvSharp.Extensions.BitmapConverter.ToMat(BitmapCapturedImage);
			if (args.isRealSize) MatCapturedImage.CopyTo(MatResizedImage);
			else Cv2.Resize(MatCapturedImage, MatResizedImage, new OpenCvSharp.Size(MatCapturedImage.Width * args.dX, MatCapturedImage.Height * args.dY), 0, 0, InterpolationFlags.Lanczos4); // resize to
			capturedsize.X = MatCapturedImage.Width;
			capturedsize.Y = MatCapturedImage.Height;
			MatCapturedImage.Dispose();
		}
		return (MatResizedImage, capturedsize);
	}

	/// <summary>
	/// GetPreviousProcess
	/// </summary>
	/// <returns></returns>
	public static Process GetPreviousProcess()
	{
		Process curProcess = Process.GetCurrentProcess();
		Process[] allProcesses = Process.GetProcessesByName(curProcess.ProcessName);

		foreach (Process checkProcess in allProcesses)
		{
			if (checkProcess.Id != curProcess.Id)
			{
				if (String.Compare(
						checkProcess.MainModule.FileName,
						curProcess.MainModule.FileName, true) == 0)
				{
					return checkProcess;
				}
			}
		}
		return null;
	}

	/// <summary>
	/// post keycode to _hWnd
	/// </summary>
	/// <param name="_hWnd"></param>
	/// <param name="_vk"></param>
	public static void KeyboardClickP(IntPtr _hWnd, VirtualKey _vk)
	{
		IntPtr lparamKEYDOWN = (IntPtr)((0x00 << 24) | (NativeMethods.MapVirtualKey((uint)_vk, 0) << 16) | 0x01);
		IntPtr lparamKEYUP = (IntPtr)((0xC0 << 24) | (NativeMethods.MapVirtualKey((uint)_vk, 0) << 16) | 0x01);

		NativeMethods.PostMessage(_hWnd, WindowMessage.WM_KEYDOWN, new IntPtr((int)_vk), lparamKEYDOWN);
		System.Threading.Thread.Sleep(20);
		NativeMethods.PostMessage(_hWnd, WindowMessage.WM_KEYUP, new IntPtr((int)_vk), lparamKEYUP);
		System.Threading.Thread.Sleep(20);
	}

	/// <summary>
	/// send keycode to _hWnd
	/// </summary>
	/// <param name="_hWnd"></param>
	/// <param name="_vk"></param>
	public static void KeyboardClickS(IntPtr _hWnd, VirtualKey _vk)
	{
		IntPtr lparamKEYDOWN = (IntPtr)((0x00 << 24) | (NativeMethods.MapVirtualKey((uint)_vk, 0) << 16) | 0x01);
		IntPtr lparamKEYUP = (IntPtr)((0xC0 << 24) | (NativeMethods.MapVirtualKey((uint)_vk, 0) << 16) | 0x01);

		NativeMethods.SendMessage(_hWnd, WindowMessage.WM_KEYDOWN, new IntPtr((int)_vk), lparamKEYDOWN);
		NativeMethods.SendMessage(_hWnd, WindowMessage.WM_KEYUP, new IntPtr((int)_vk), lparamKEYUP);
	}

	/// <summary>
	/// click point(_X, _y) in _hWnd
	/// </summary>
	/// <param name="_hWnd"></param>
	/// <param name="_x"></param>
	/// <param name="_y"></param>
	public static void MouseLeftClick(IntPtr _hWnd, double _x, double _y)
	{
		Task task = Task.Factory.StartNew(() =>
		{
			IntPtr pos = new IntPtr(MakeDWord((ushort)_x, (ushort)_y));
			IntPtr setcursorlbuttondownlparam = new IntPtr(MakeDWord((ushort)NCHITTEST.HTCLIENT, (ushort)WindowMessage.WM_LBUTTONDOWN));
			IntPtr setcursormousemovelparam = new IntPtr(MakeDWord((ushort)NCHITTEST.HTCLIENT, (ushort)WindowMessage.WM_MOUSEMOVE));

			NativeMethods.SendNotifyMessage(_hWnd, WindowMessage.WM_SETCURSOR, _hWnd, setcursormousemovelparam);
			NativeMethods.PostMessage(_hWnd, WindowMessage.WM_MOUSEMOVE, IntPtr.Zero, pos);
			NativeMethods.SendNotifyMessage(_hWnd, WindowMessage.WM_SETCURSOR, _hWnd, setcursorlbuttondownlparam);
			NativeMethods.PostMessage(_hWnd, WindowMessage.WM_LBUTTONDOWN, new IntPtr((int)fwKeys.MK_LBUTTON), pos);
			NativeMethods.PostMessage(_hWnd, WindowMessage.WM_LBUTTONUP, IntPtr.Zero, pos);
			NativeMethods.SendNotifyMessage(_hWnd, WindowMessage.WM_SETCURSOR, _hWnd, setcursormousemovelparam);
			NativeMethods.PostMessage(_hWnd, WindowMessage.WM_LBUTTONUP, IntPtr.Zero, pos);
			System.Threading.Thread.Sleep(25);
		});
		task.Wait();
	}

	/// <summary>
	/// invoke mouse event
	/// </summary>
	/// <param name="_hWnd"></param>
	/// <param name="mouseevent"></param>
	/// <param name="_x"></param>
	/// <param name="_y"></param>
	/// <param name="wheel"></param>
	/// <param name="fwKeys"></param>
	public static bool MouseEvent(IntPtr _hWnd, MOUSEEVENT mouseevent = MOUSEEVENT.CLICK, double _x = 0, double _y = 0, int wheel = 0, fwKeys fwKeys = fwKeys.MK_LBUTTON)
	{
		if (_hWnd == IntPtr.Zero) return false;

		WindowMessage _up = WindowMessage.WM_LBUTTONUP;
		WindowMessage _down = WindowMessage.WM_LBUTTONDOWN;
		WindowMessage _dclk = WindowMessage.WM_LBUTTONDBLCLK;

		IntPtr pos = new IntPtr(MakeDWord((ushort)_x, (ushort)_y));
		IntPtr setcursorbuttondownlparam = new IntPtr(MakeDWord((ushort)NCHITTEST.HTCLIENT, (ushort)WindowMessage.WM_LBUTTONDOWN));
		IntPtr setcursormousemovelparam = new IntPtr(MakeDWord((ushort)NCHITTEST.HTCLIENT, (ushort)WindowMessage.WM_MOUSEMOVE));

		switch (fwKeys)
		{
			case fwKeys.MK_LBUTTON:
				_up = WindowMessage.WM_LBUTTONUP;
				_down = WindowMessage.WM_LBUTTONDOWN;
				_dclk = WindowMessage.WM_LBUTTONDBLCLK;
				break;
			case fwKeys.MK_RBUTTON:
				_up = WindowMessage.WM_RBUTTONUP;
				_down = WindowMessage.WM_RBUTTONDOWN;
				_dclk = WindowMessage.WM_RBUTTONDBLCLK;
				break;
			case fwKeys.MK_MBUTTON:
				_up = WindowMessage.WM_MBUTTONUP;
				_down = WindowMessage.WM_MBUTTONDOWN;
				_dclk = WindowMessage.WM_MBUTTONDBLCLK;
				break;
			default:
				return false;
				break;
		}
		switch (mouseevent)
		{
			case MOUSEEVENT.CLICK:
				setcursorbuttondownlparam = new IntPtr(MakeDWord((ushort)NCHITTEST.HTCLIENT, (ushort)_down));
				setcursormousemovelparam = new IntPtr(MakeDWord((ushort)NCHITTEST.HTCLIENT, (ushort)WindowMessage.WM_MOUSEMOVE));
				//NativeMethods.SendNotifyMessage(_hWnd, WindowMessage.WM_SETCURSOR, _hWnd, setcursormousemovelparam);
				NativeMethods.PostMessage(_hWnd, WindowMessage.WM_MOUSEMOVE, IntPtr.Zero, pos);
				NativeMethods.SendNotifyMessage(_hWnd, WindowMessage.WM_SETCURSOR, _hWnd, setcursorbuttondownlparam);
				NativeMethods.PostMessage(_hWnd, _down, new IntPtr((int)fwKeys), pos);
				System.Threading.Thread.Sleep(50);
				NativeMethods.PostMessage(_hWnd, _up, IntPtr.Zero, pos);
				NativeMethods.SendNotifyMessage(_hWnd, WindowMessage.WM_SETCURSOR, _hWnd, setcursormousemovelparam);
				NativeMethods.PostMessage(_hWnd, WindowMessage.WM_MOUSEMOVE, IntPtr.Zero, pos);
				break;
			case MOUSEEVENT.DOUBLECLICK:
				setcursorbuttondownlparam = new IntPtr(MakeDWord((ushort)NCHITTEST.HTCLIENT, (ushort)_down));
				setcursormousemovelparam = new IntPtr(MakeDWord((ushort)NCHITTEST.HTCLIENT, (ushort)WindowMessage.WM_MOUSEMOVE));
				NativeMethods.SendNotifyMessage(_hWnd, WindowMessage.WM_SETCURSOR, _hWnd, setcursormousemovelparam);
				NativeMethods.PostMessage(_hWnd, _dclk, new IntPtr(MakeDWord(0, (int)WindowMessage.WHEEL_DELTA * wheel)), pos);
				break;
			case MOUSEEVENT.TRIPLECLICK:
				break;
			case MOUSEEVENT.WHEEL:
				setcursorbuttondownlparam = new IntPtr(MakeDWord((ushort)NCHITTEST.HTCLIENT, (ushort)WindowMessage.WM_LBUTTONDOWN));
				setcursormousemovelparam = new IntPtr(MakeDWord((ushort)NCHITTEST.HTCLIENT, (ushort)WindowMessage.WM_MOUSEMOVE));
				//NativeMethods.SetForegroundWindow(_hWnd);
				//System.Threading.Thread.Sleep(1000);
				NativeMethods.SetCursorPos((int)_x, (int)_y);
				//System.Threading.Thread.Sleep(1000);
				//NativeMethods.SendNotifyMessage(_hWnd, WindowMessage.WM_SETCURSOR, _hWnd, setcursormousemovelparam);
				NativeMethods.PostMessage(_hWnd, WindowMessage.WM_MOUSEWHEEL, new IntPtr(MakeDWord(0, (int)WindowMessage.WHEEL_DELTA * wheel)), pos);
				break;
			case MOUSEEVENT.MOVE:
				if (IntPtr.Zero == _hWnd)
				{
					NativeMethods.SetCursorPos((int)_x, (int)_y);
				}
				else
				{
					setcursormousemovelparam = new IntPtr(MakeDWord((ushort)NCHITTEST.HTCLIENT, (ushort)WindowMessage.WM_MOUSEMOVE));
					NativeMethods.SendNotifyMessage(_hWnd, WindowMessage.WM_SETCURSOR, _hWnd, setcursormousemovelparam);
					NativeMethods.PostMessage(_hWnd, WindowMessage.WM_MOUSEMOVE, IntPtr.Zero, pos);
				}
				break;
			default:
				return false;
				break;
		}
		return true;
	}
	/// <summary>
	/// 
	/// </summary>
	/// <param name="_hWnd"></param>
	public static void ForceActive(IntPtr _hWnd)
	{
		if (NativeMethods.IsIconic(_hWnd))
			NativeMethods.ShowWindowAsync(_hWnd, nCmdShow.SW_RESTORE);

		uint processId = 0;
		uint foregroundID = NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(), out processId);
		uint targetID = NativeMethods.GetWindowThreadProcessId(_hWnd, out processId);

		NativeMethods.AttachThreadInput(targetID, foregroundID, true);
		NativeMethods.SetForegroundWindow(_hWnd);
		NativeMethods.AttachThreadInput(targetID, foregroundID, false);
	}

	/// <summary>
	/// CalculateAbsoluteCoordinateX
	/// </summary>
	/// <param name="x"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CalculateAbsoluteCoordinateX(int x)
	{
		return (x * 65536) / NativeMethods.GetSystemMetrics(SystemMetric.SM_CXSCREEN);
	}

	/// <summary>
	/// CalculateAbsoluteCoordinateY
	/// </summary>
	/// <param name="y"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CalculateAbsoluteCoordinateY(int y)
	{
		return (y * 65536) / NativeMethods.GetSystemMetrics(SystemMetric.SM_CYSCREEN);
	}

	/// <summary>
	/// Make HighWord
	/// </summary>
	/// <param name="_word"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long HighWord(int _word)
	{
		return ((_word & 0xFFFF0000) >> 16);
	}

	/// <summary>
	/// Make LowWord
	/// </summary>
	/// <param name="_word"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long LowWord(int _word)
	{
		return (_word & 0x0000FFFF);
	}

	/// <summary>
	/// Make DWord
	/// </summary>
	/// <param name="_low"></param>
	/// <param name="_high"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long MakeDWord(long _low, long _high)
	{
		return _high << 16 | (_low & 0xffff);
	}

	public static Bitmap GetWindowCaptureHdc(IntPtr hWnd)
	{
		RECT rc = new RECT();
		if (!NativeMethods.GetClientRect(hWnd, ref rc)) return null;

		// if minimum error
		if (rc.right == 0 && rc.bottom == 0) return null;

		// create a bitmap from the visible clipping bounds of the graphics object from the window
		Bitmap bmpCapture = new Bitmap(rc.right - rc.left, rc.bottom - rc.top);

		try
		{
			// create a graphics object from the bitmap
			Graphics gfxBitmap = Graphics.FromImage(bmpCapture);

			// get a device context for the bitmap
			IntPtr hdcBitmap = gfxBitmap.GetHdc();

			// get a device context for the window
			//IntPtr hdcWindow = NativeMethods.GetWindowDC(hWnd);
			IntPtr hdcWindow = NativeMethods.GetDC(hWnd);

			// bitblt the window to the bitmap
			NativeMethods.BitBlt(hdcBitmap, 0, 0, rc.right - rc.left, rc.bottom - rc.top, hdcWindow, 0, 0, TernaryRasterOperations.SRCCOPY);

			// release the bitmap's device context
			gfxBitmap.ReleaseHdc(hdcBitmap);

			NativeMethods.ReleaseDC(hWnd, hdcWindow);

			// dispose of the bitmap's graphics object
			gfxBitmap.Dispose();
		}
		catch
		{
			bmpCapture = null;
		}
		// return the bitmap of the window
		return bmpCapture;
	}

	/// <summary>
	/// Get Window Capture of _hWnd
	/// </summary>
	/// <param name="_hWnd"></param>
	/// <param name="rectangle"></param>
	/// <returns></returns>
	public static Bitmap GetWindowCapture(IntPtr _hWnd)
	{
		Rectangle rectangle = new Rectangle();
		RECT clientRect = new RECT();
		Bitmap bmpCapture = null;
		try
		{
			// target app to topmost for capture
			NativeMethods.ShowWindow(_hWnd, nCmdShow.SW_RESTORE);
			NativeMethods.SetWindowPos(_hWnd, (IntPtr)hWndInsertAfter.HWND_TOPMOST, 0, 0, 0, 0, SetWindowPosFlags.DoNotActivate | SetWindowPosFlags.IgnoreResize | SetWindowPosFlags.IgnoreMove | SetWindowPosFlags.DoNotSendChangingEvent | SetWindowPosFlags.ShowWindow);

			// begin capture
			System.Drawing.Point screenPoint = new System.Drawing.Point(0, 0);
			NativeMethods.ClientToScreen(_hWnd, ref screenPoint);
			NativeMethods.GetClientRect(_hWnd, ref clientRect);
			rectangle = new Rectangle(clientRect.left, clientRect.top, clientRect.right - clientRect.left, clientRect.bottom - clientRect.top);
			System.Drawing.Point captureStartPoint = new System.Drawing.Point(screenPoint.X + rectangle.X, screenPoint.Y + rectangle.Y);
			bmpCapture = new Bitmap(rectangle.Width, rectangle.Height);
			Graphics graphics = Graphics.FromImage(bmpCapture);
			graphics.CopyFromScreen(captureStartPoint, new System.Drawing.Point(0, 0), rectangle.Size);

			// target app to nottopmost
			NativeMethods.SetWindowPos(_hWnd, (IntPtr)hWndInsertAfter.HWND_NOTOPMOST, 0, 0, 0, 0, SetWindowPosFlags.DoNotActivate | SetWindowPosFlags.IgnoreResize | SetWindowPosFlags.IgnoreMove | SetWindowPosFlags.DoNotSendChangingEvent | SetWindowPosFlags.ShowWindow);
		}
		catch
		{
			bmpCapture = null;
		}
		return bmpCapture;
	}
}

internal class NativeMethods
{
	[DllImport("user32.dll")]
	internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll")]
	internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

	[DllImport("user32.dll")]
	internal static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

	[DllImport("user32.dll", SetLastError = false)]
	internal static extern IntPtr GetDesktopWindow();

	[DllImport("user32.dll")]
	internal static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

	[DllImport("user32.dll")]
	internal static extern bool DrawMenuBar(IntPtr hWnd);

	[DllImport("user32.dll")]
	internal static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	internal static extern bool AppendMenu(IntPtr hMenu, MenuFlags uFlags, uint uIDNewItem, string lpNewItem);

	[DllImport("gdi32.dll", EntryPoint = "BitBlt", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool BitBlt([In] IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, [In] IntPtr hdcSrc, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);

	[DllImport("user32.dll")]
	internal static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

	[DllImport("user32.dll")]
	internal static extern IntPtr GetWindowDC(IntPtr hWnd);

	[DllImport("user32.dll")]
	internal static extern IntPtr GetDC(IntPtr hWnd);

	[System.Runtime.InteropServices.DllImport("gdi32.dll")]
	internal static extern bool DeleteObject(System.IntPtr hObject);

	[DllImport("kernel32.dll")]
	internal static extern uint GetLastError();

	[DllImport("user32.dll")]
	internal static extern bool ClientToScreen(IntPtr hWnd, ref System.Drawing.Point lpPoint);

	[DllImport("user32.dll", SetLastError = true)]
	internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

	[DllImport("user32.dll", SetLastError = true)]
	internal static extern bool BringWindowToTop(IntPtr hWnd);

	[DllImport("user32.dll")]
	internal static extern bool IsIconic(IntPtr hWnd);

	[DllImport("user32.dll")]
	internal static extern bool ShowWindowAsync(IntPtr hWnd, nCmdShow cmdShow);

	[DllImport("user32.dll")]
	internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

	[DllImport("kernel32.dll", SetLastError = true)]
	internal static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

	[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	internal static extern bool QueryFullProcessImageName([In]IntPtr hProcess, [In]int dwFlags, [Out]StringBuilder lpExeName, ref int lpdwSize);

	[DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
	internal static extern int EnumProcessModules(IntPtr hProcess, [Out] IntPtr lphModule, uint cb, out uint lpcbNeeded);

	[DllImport("psapi.dll", CharSet = CharSet.Auto)]
	internal static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, [In] [MarshalAs(UnmanagedType.U4)] int nSize);

	[DllImport("psapi.dll", CharSet = CharSet.Auto)]
	internal static extern uint GetModuleBaseName(IntPtr hProcess, IntPtr hModule, StringBuilder lpBaseName, uint nSize);

	[DllImport("user32.dll")]
	internal static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

	[DllImport("user32.dll")]
	internal extern static int GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

	[DllImport("user32.dll")]
	internal extern static IntPtr GetForegroundWindow();

	[DllImport("user32.dll", SetLastError = true)]
	internal static extern IntPtr SetFocus(IntPtr hWnd);

	[DllImport("User32.dll")]
	internal static extern bool SetCapture(IntPtr hWnd);

	[DllImport("user32.dll")]
	internal static extern bool ReleaseCapture();

	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	internal static extern bool SendNotifyMessage(IntPtr hWnd, WindowMessage Msg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll", CharSet = CharSet.Auto, PreserveSig = false, SetLastError = true)]
	internal static extern void PostMessage(IntPtr windowhWnd, WindowMessage message, IntPtr wparam, IntPtr lparam);

	//    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	//    internal static extern IntPtr SendMessage(IntPtr windowhWnd, WindowMessage message, IntPtr wparam, IntPtr lparam);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	internal static extern int SendMessage(IntPtr windowhWnd, WindowMessage message, IntPtr wparam, IntPtr lparam);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	internal static extern int SendMessage(IntPtr windowhWnd, WindowMessage message, IntPtr wparam, StringBuilder lparam);

	[DllImport("user32.dll")]
	internal static extern bool SetForegroundWindow(IntPtr hWnd);

	//[DllImport("user32.dll")]
	//internal static extern void SendInput(int nInputs, ref INPUT pInputs, int cbsize);

	[DllImport("user32.dll")]
	internal static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

	[DllImport("user32.dll", EntryPoint = "MapVirtualKeyA")]
	internal static extern uint MapVirtualKey(uint wCode, uint wMapType);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

	[DllImport("User32.Dll", EntryPoint = "GetWindowRect")]
	internal static extern bool GetWindowRect(IntPtr hwnd, ref RECT rc);

	[DllImport("User32.dll")]
	internal static extern bool GetClientRect(IntPtr hWnd, ref RECT rc);

	[DllImport("user32.dll")]
	internal static extern bool ShowWindow(IntPtr hWnd, nCmdShow cmdShow);

	[DllImport("USER32.dll")]
	internal static extern void SetCursorPos(int X, int Y);

	[DllImport("USER32.dll")]
	internal static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

	[DllImport("user32.dll")]
	internal static extern int GetSystemMetrics(SystemMetric smIndex);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool GetCursorPos(out POINT lpPoint);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	internal static extern int GetClassName(IntPtr hWnd, StringBuilder className, int maxCount);

	[DllImport("user32.dll")]
	internal static extern IntPtr WindowFromPoint(POINT point);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
}

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct WINDOWPLACEMENT
{
	public int length;
	public int flags;
	public SW showCmd;
	public POINT minPosition;
	public POINT maxPosition;
	public RECT normalPosition;
}

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
	public int X;
	public int Y;

	public POINT(int x, int y)
	{
		this.X = x;
		this.Y = y;
	}
}

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
	public int left;
	public int top;
	public int right;
	public int bottom;

	public RECT(int left, int top, int right, int bottom)
	{
		this.left = left;
		this.top = top;
		this.right = right;
		this.bottom = bottom;
	}
}

public enum MOUSEEVENT
{
	NOTHING = 0x00,
	CLICK = 0x01,
	DOUBLECLICK = 0x02,
	TRIPLECLICK = 0x03,
	WHEEL = 0x04,
	MOVE = 0x10
}

public enum SW
{
	HIDE = 0,
	SHOWNORMAL = 1,
	SHOWMINIMIZED = 2,
	SHOWMAXIMIZED = 3,
	SHOWNOACTIVATE = 4,
	SHOW = 5,
	MINIMIZE = 6,
	SHOWMINNOACTIVE = 7,
	SHOWNA = 8,
	RESTORE = 9,
	SHOWDEFAULT = 10,
}

/*
// Declare the POINT struct
[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;

    public static implicit operator Point(POINT point)
    {
        return new Point(point.X, point.Y);
    }
}
*/
/*
// Declare the RECT struct
[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int left;
    public int top;
    public int right;
    public int bottom;
}
*/

// Declare the INPUT struct
[StructLayout(LayoutKind.Sequential)]
public struct INPUT
{
	internal uint type;
	internal InputUnion U;
	internal static int Size
	{
		get { return Marshal.SizeOf(typeof(INPUT)); }
	}
}

// Declare the InputUnion struct
[StructLayout(LayoutKind.Explicit)]
public struct InputUnion
{
	[FieldOffset(0)]
	internal MOUSEINPUT mi;
	[FieldOffset(0)]
	internal KEYBDINPUT ki;
	[FieldOffset(0)]
	internal HARDWAREINPUT hi;
}

[StructLayout(LayoutKind.Sequential)]
public struct MOUSEINPUT
{
	internal int dx;
	internal int dy;
	internal MouseEventDataXButtons mouseData;
	internal MOUSEEVENTF dwFlags;
	internal uint time;
	internal UIntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
public struct KEYBDINPUT
{
	internal VirtualKey wVk;
	internal ScanCode wScan;
	internal KEYEVENTF dwFlags;
	internal int time;
	internal UIntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
public struct HARDWAREINPUT
{
	internal int uMsg;
	internal short wParamL;
	internal short wParamH;
}

[Flags]
public enum MenuFlags : uint
{
	MF_STRING = 0x00000000,
	MFS_GLAYED = 0x00000003,
	MF_BITMAP = 0x00000004,
	MFS_CHECKED = 0x00000008,
	MF_POPUP = 0x00000010,
	MF_MENUBARBREAK = 0x00000020,
	MF_MENUBREAK = 0x00000040,
	MF_OWNERDRAW = 0x00000100,
	MF_SEPARATOR = 0x00000800
}

[Flags]
public enum ProcessAccessFlags : uint
{
	All = 0x001F0FFF,
	Terminate = 0x00000001,
	CreateThread = 0x00000002,
	VMOperation = 0x00000008,
	PROCESS_VM_READ = 0x10,
	VMWrite = 0x00000020,
	DupHandle = 0x00000040,
	SetInformation = 0x00000200,
	QueryInformation = 0x00000400,
	Synchronize = 0x00100000
}

[Flags]
public enum nCmdShow : int
{
	SW_HIDE = 0,
	SW_SHOWNORMAL = 1,
	SW_NORMAL = 1,
	SW_SHOWMINIMIZED = 2,
	SW_SHOWMAXIMIZED = 3,
	SW_MAXIMIZE = 3,
	SW_SHOWNOACTIVATE = 4,
	SW_SHOW = 5,
	SW_MINIMIZE = 6,
	SW_SHOWMINNOACTIVE = 7,
	SW_SHOWNA = 8,
	SW_RESTORE = 9,
	SW_SHOWDEFAULT = 10,
	SW_FORCEMINIMIZE = 11,
	SW_MAX = 11
}

[Flags]
public enum fwKeys : int
{
	MK_LBUTTON = 0x0001,
	MK_RBUTTON = 0x00002,
	MK_SHIFT = 0x0003,
	MK_CONTROL = 0x0008,
	MK_MBUTTON = 0x0010,
	MK_XBUTTON1 = 0x0020,
	MK_XBUTTON2 = 0x0022
}

[Flags]
public enum TernaryRasterOperations : uint
{
	/// <summary>dest = source</summary>
	SRCCOPY = 0x00CC0020,
	/// <summary>dest = source OR dest</summary>
	SRCPAINT = 0x00EE0086,
	/// <summary>dest = source AND dest</summary>
	SRCAND = 0x008800C6,
	/// <summary>dest = source XOR dest</summary>
	SRCINVERT = 0x00660046,
	/// <summary>dest = source AND (NOT dest)</summary>
	SRCERASE = 0x00440328,
	/// <summary>dest = (NOT source)</summary>
	NOTSRCCOPY = 0x00330008,
	/// <summary>dest = (NOT src) AND (NOT dest)</summary>
	NOTSRCERASE = 0x001100A6,
	/// <summary>dest = (source AND pattern)</summary>
	MERGECOPY = 0x00C000CA,
	/// <summary>dest = (NOT source) OR dest</summary>
	MERGEPAINT = 0x00BB0226,
	/// <summary>dest = pattern</summary>
	PATCOPY = 0x00F00021,
	/// <summary>dest = DPSnoo</summary>
	PATPAINT = 0x00FB0A09,
	/// <summary>dest = pattern XOR dest</summary>
	PATINVERT = 0x005A0049,
	/// <summary>dest = (NOT dest)</summary>
	DSTINVERT = 0x00550009,
	/// <summary>dest = BLACK</summary>
	BLACKNESS = 0x00000042,
	/// <summary>dest = WHITE</summary>
	WHITENESS = 0x00FF0062,
	/// <summary>
	/// Capture window as seen on screen.  This includes layered windows
	/// such as WPF windows with AllowsTransparency="true"
	/// </summary>
	CAPTUREBLT = 0x40000000
}

//
// Window Messages
//
[Flags]
public enum WindowMessage
{
	WM_NULL = 0x0000,
	WM_CREATE = 0x0001,
	WM_DESTROY = 0x0002,
	WM_MOVE = 0x0003,
	WM_SIZE = 0x0005,
	WM_ACTIVATE = 0x0006,
	WA_INACTIVE = 0,
	WA_ACTIVE = 1,
	WA_CLICKACTIVE = 2,
	WM_SETFOCUS = 0x0007,
	WM_KILLFOCUS = 0x0008,
	WM_ENABLE = 0x000A,
	WM_SETREDRAW = 0x000B,
	WM_SETTEXT = 0x000C,
	WM_GETTEXT = 0x000D,
	WM_GETTEXTLENGTH = 0x000E,
	WM_PAINT = 0x000F,
	WM_CLOSE = 0x0010,
	WM_QUERYENDSESSION = 0x0011,
	WM_QUERYOPEN = 0x0013,
	WM_ENDSESSION = 0x0016,
	WM_QUIT = 0x0012,
	WM_ERASEBKGND = 0x0014,
	WM_SYSCOLORCHANGE = 0x0015,
	WM_SHOWWINDOW = 0x0018,
	WM_WININICHANGE = 0x001A,
	WM_SETTINGCHANGE = WM_WININICHANGE,
	WM_DEVMODECHANGE = 0x001B,
	WM_ACTIVATEAPP = 0x001C,
	WM_FONTCHANGE = 0x001D,
	WM_TIMECHANGE = 0x001E,
	WM_CANCELMODE = 0x001F,
	WM_SETCURSOR = 0x0020,
	WM_MOUSEACTIVATE = 0x0021,
	WM_CHILDACTIVATE = 0x0022,
	WM_QUEUESYNC = 0x0023,
	WM_GETMINMAXINFO = 0x0024,
	WM_PAINTICON = 0x0026,
	WM_ICONERASEBKGND = 0x0027,
	WM_NEXTDLGCTL = 0x0028,
	WM_SPOOLERSTATUS = 0x002A,
	WM_DRAWITEM = 0x002B,
	WM_MEASUREITEM = 0x002C,
	WM_DELETEITEM = 0x002D,
	WM_VKEYTOITEM = 0x002E,
	WM_CHARTOITEM = 0x002F,
	WM_SETFONT = 0x0030,
	WM_GETFONT = 0x0031,
	WM_SETHOTKEY = 0x0032,
	WM_GETHOTKEY = 0x0033,
	WM_QUERYDRAGICON = 0x0037,
	WM_COMPAREITEM = 0x0039,
	WM_GETOBJECT = 0x003D,
	WM_COMPACTING = 0x0041,
	WM_COMMNOTIFY = 0x0044,
	WM_WINDOWPOSCHANGING = 0x0046,
	WM_WINDOWPOSCHANGED = 0x0047,
	WM_POWER = 0x0048,
	PWR_OK = 1,
	PWR_FAIL = (-1),
	PWR_SUSPENDREQUEST = 1,
	PWR_SUSPENDRESUME = 2,
	PWR_CRITICALRESUME = 3,
	WM_COPYDATA = 0x004A,
	WM_CANCELJOURNAL = 0x004B,
	WM_NOTIFY = 0x004E,
	WM_INPUTLANGCHANGEREQUEST = 0x0050,
	WM_INPUTLANGCHANGE = 0x0051,
	WM_TCARD = 0x0052,
	WM_HELP = 0x0053,
	WM_USERCHANGED = 0x0054,
	WM_NOTIFYFORMAT = 0x0055,
	NFR_ANSI = 1,
	NFR_UNICODE = 2,
	NF_QUERY = 3,
	NF_REQUERY = 4,
	WM_CONTEXTMENU = 0x007B,
	WM_STYLECHANGING = 0x007C,
	WM_STYLECHANGED = 0x007D,
	WM_DISPLAYCHANGE = 0x007E,
	WM_GETICON = 0x007F,
	WM_SETICON = 0x0080,
	WM_NCCREATE = 0x0081,
	WM_NCDESTROY = 0x0082,
	WM_NCCALCSIZE = 0x0083,
	WM_NCHITTEST = 0x0084,
	WM_NCPAINT = 0x0085,
	WM_NCACTIVATE = 0x0086,
	WM_GETDLGCODE = 0x0087,
	WM_SYNCPAINT = 0x0088,
	WM_NCMOUSEMOVE = 0x00A0,
	WM_NCLBUTTONDOWN = 0x00A1,
	WM_NCLBUTTONUP = 0x00A2,
	WM_NCLBUTTONDBLCLK = 0x00A3,
	WM_NCRBUTTONDOWN = 0x00A4,
	WM_NCRBUTTONUP = 0x00A5,
	WM_NCRBUTTONDBLCLK = 0x00A6,
	WM_NCMBUTTONDOWN = 0x00A7,
	WM_NCMBUTTONUP = 0x00A8,
	WM_NCMBUTTONDBLCLK = 0x00A9,
	WM_NCXBUTTONDOWN = 0x00AB,
	WM_NCXBUTTONUP = 0x00AC,
	WM_NCXBUTTONDBLCLK = 0x00AD,
	WM_INPUT_DEVICE_CHANGE = 0x00FE,
	WM_INPUT = 0x00FF,
	WM_KEYFIRST = 0x0100,
	WM_KEYDOWN = 0x0100,
	WM_KEYUP = 0x0101,
	WM_CHAR = 0x0102,
	WM_DEADCHAR = 0x0103,
	WM_SYSKEYDOWN = 0x0104,
	WM_SYSKEYUP = 0x0105,
	WM_SYSCHAR = 0x0106,
	WM_SYSDEADCHAR = 0x0107,
	WM_UNICHAR = 0x0109,
	WM_KEYLAST = 0x0109,
	UNICODE_NOCHAR = 0xFFFF,
	// WM_KEYLAST = 0x0108, // _WIN32_WINNT < 0x0501
	WM_IME_STARTCOMPOSITION = 0x010D,
	WM_IME_ENDCOMPOSITION = 0x010E,
	WM_IME_COMPOSITION = 0x010F,
	WM_IME_KEYLAST = 0x010F,
	WM_INITDIALOG = 0x0110,
	WM_COMMAND = 0x0111,
	WM_SYSCOMMAND = 0x0112,
	WM_TIMER = 0x0113,
	WM_HSCROLL = 0x0114,
	WM_VSCROLL = 0x0115,
	WM_INITMENU = 0x0116,
	WM_INITMENUPOPUP = 0x0117,
	WM_GESTURE = 0x0119,
	WM_GESTURENOTIFY = 0x011A,
	WM_MENUSELECT = 0x011F,
	WM_MENUCHAR = 0x0120,
	WM_ENTERIDLE = 0x0121,
	WM_MENURBUTTONUP = 0x0122,
	WM_MENUDRAG = 0x0123,
	WM_MENUGETOBJECT = 0x0124,
	WM_UNINITMENUPOPUP = 0x0125,
	WM_MENUCOMMAND = 0x0126,
	WM_CHANGEUISTATE = 0x0127,
	WM_UPDATEUISTATE = 0x0128,
	WM_QUERYUISTATE = 0x0129,
	UIS_SET = 1,
	UIS_CLEAR = 2,
	UIS_INITIALIZE = 3,
	UISF_HIDEFOCUS = 0x1,
	UISF_HIDEACCEL = 0x2,
	UISF_ACTIVE = 0x4,
	WM_CTLCOLORMSGBOX = 0x0132,
	WM_CTLCOLOREDIT = 0x0133,
	WM_CTLCOLORLISTBOX = 0x0134,
	WM_CTLCOLORBTN = 0x0135,
	WM_CTLCOLORDLG = 0x0136,
	WM_CTLCOLORSCROLLBAR = 0x0137,
	WM_CTLCOLORSTATIC = 0x0138,
	MN_GETHMENU = 0x01E1,
	WM_MOUSEFIRST = 0x0200,
	WM_MOUSEMOVE = 0x0200,
	WM_LBUTTONDOWN = 0x0201,
	WM_LBUTTONUP = 0x0202,
	WM_LBUTTONDBLCLK = 0x0203,
	WM_RBUTTONDOWN = 0x0204,
	WM_RBUTTONUP = 0x0205,
	WM_RBUTTONDBLCLK = 0x0206,
	WM_MBUTTONDOWN = 0x0207,
	WM_MBUTTONUP = 0x0208,
	WM_MBUTTONDBLCLK = 0x0209,
	WM_MOUSEWHEEL = 0x020A,
	WM_XBUTTONDOWN = 0x020B,
	WM_XBUTTONUP = 0x020C,
	WM_XBUTTONDBLCLK = 0x020D,
	WM_MOUSEHWHEEL = 0x020E,
	WM_MOUSELAST = 0x020E,
	//WM_MOUSELAST = 0x020D,  // _WIN32_WINNT >= 0x0500
	//WM_MOUSELAST = 0x020A,  // _WIN32_WINNT >= 0x0400) || (_WIN32_WINDOWS > 0x0400
	//WM_MOUSELAST = 0x0209,  // others
	WHEEL_DELTA = 120,
	//GET_WHEEL_DELTA_WPARAM(wParam) = ((short)HIWORD(wParam)),
	//WHEEL_PAGESCROLL = (UINT_MAX),
	//GET_KEYSTATE_WPARAM(wParam) = (LOWORD(wParam)),
	//GET_NCHITTEST_WPARAM(wParam) = ((short)LOWORD(wParam)),
	//GET_XBUTTON_WPARAM(wParam) = (HIWORD(wParam)),
	XBUTTON1 = 0x0001,
	XBUTTON2 = 0x0002,
	WM_PARENTNOTIFY = 0x0210,
	WM_ENTERMENULOOP = 0x0211,
	WM_EXITMENULOOP = 0x0212,
	WM_NEXTMENU = 0x0213,
	WM_SIZING = 0x0214,
	WM_CAPTURECHANGED = 0x0215,
	WM_MOVING = 0x0216,
	WM_POWERBROADCAST = 0x0218,
	PBT_APMQUERYSUSPEND = 0x0000,
	PBT_APMQUERYSTANDBY = 0x0001,
	PBT_APMQUERYSUSPENDFAILED = 0x0002,
	PBT_APMQUERYSTANDBYFAILED = 0x0003,
	PBT_APMSUSPEND = 0x0004,
	PBT_APMSTANDBY = 0x0005,
	PBT_APMRESUMECRITICAL = 0x0006,
	PBT_APMRESUMESUSPEND = 0x0007,
	PBT_APMRESUMESTANDBY = 0x0008,
	PBTF_APMRESUMEFROMFAILURE = 0x00000001,
	PBT_APMBATTERYLOW = 0x0009,
	PBT_APMPOWERSTATUSCHANGE = 0x000A,
	PBT_APMOEMEVENT = 0x000B,
	PBT_APMRESUMEAUTOMATIC = 0x0012,
	PBT_POWERSETTINGCHANGE = 0x8013,
	WM_DEVICECHANGE = 0x0219,
	WM_MDICREATE = 0x0220,
	WM_MDIDESTROY = 0x0221,
	WM_MDIACTIVATE = 0x0222,
	WM_MDIRESTORE = 0x0223,
	WM_MDINEXT = 0x0224,
	WM_MDIMAXIMIZE = 0x0225,
	WM_MDITILE = 0x0226,
	WM_MDICASCADE = 0x0227,
	WM_MDIICONARRANGE = 0x0228,
	WM_MDIGETACTIVE = 0x0229,
	WM_MDISETMENU = 0x0230,
	WM_ENTERSIZEMOVE = 0x0231,
	WM_EXITSIZEMOVE = 0x0232,
	WM_DROPFILES = 0x0233,
	WM_MDIREFRESHMENU = 0x0234,
	WM_POINTERDEVICECHANGE = 0x238,
	WM_POINTERDEVICEINRANGE = 0x239,
	WM_POINTERDEVICEOUTOFRANGE = 0x23A,
	WM_TOUCH = 0x0240,
	WM_NCPOINTERUPDATE = 0x0241,
	WM_NCPOINTERDOWN = 0x0242,
	WM_NCPOINTERUP = 0x0243,
	WM_POINTERUPDATE = 0x0245,
	WM_POINTERDOWN = 0x0246,
	WM_POINTERUP = 0x0247,
	WM_POINTERENTER = 0x0249,
	WM_POINTERLEAVE = 0x024A,
	WM_POINTERACTIVATE = 0x024B,
	WM_POINTERCAPTURECHANGED = 0x024C,
	WM_TOUCHHITTESTING = 0x024D,
	WM_POINTERWHEEL = 0x024E,
	WM_POINTERHWHEEL = 0x024F,
	DM_POINTERHITTEST = 0x0250,
	WM_POINTERROUTEDTO = 0x0251,
	WM_POINTERROUTEDAWAY = 0x0252,
	WM_POINTERROUTEDRELEASED = 0x0253,
	WM_IME_SETCONTEXT = 0x0281,
	WM_IME_NOTIFY = 0x0282,
	WM_IME_CONTROL = 0x0283,
	WM_IME_COMPOSITIONFULL = 0x0284,
	WM_IME_SELECT = 0x0285,
	WM_IME_CHAR = 0x0286,
	WM_IME_REQUEST = 0x0288,
	WM_IME_KEYDOWN = 0x0290,
	WM_IME_KEYUP = 0x0291,
	WM_MOUSEHOVER = 0x02A1,
	WM_MOUSELEAVE = 0x02A3,
	WM_NCMOUSEHOVER = 0x02A0,
	WM_NCMOUSELEAVE = 0x02A2,
	WM_WTSSESSION_CHANGE = 0x02B1,
	WM_TABLET_FIRST = 0x02c0,
	WM_TABLET_LAST = 0x02df,
	WM_DPICHANGED = 0x02E0,
	WM_DPICHANGED_BEFOREPARENT = 0x02E2,
	WM_DPICHANGED_AFTERPARENT = 0x02E3,
	WM_GETDPISCALEDSIZE = 0x02E4,
	WM_CUT = 0x0300,
	WM_COPY = 0x0301,
	WM_PASTE = 0x0302,
	WM_CLEAR = 0x0303,
	WM_UNDO = 0x0304,
	WM_RENDERFORMAT = 0x0305,
	WM_RENDERALLFORMATS = 0x0306,
	WM_DESTROYCLIPBOARD = 0x0307,
	WM_DRAWCLIPBOARD = 0x0308,
	WM_PAINTCLIPBOARD = 0x0309,
	WM_VSCROLLCLIPBOARD = 0x030A,
	WM_SIZECLIPBOARD = 0x030B,
	WM_ASKCBFORMATNAME = 0x030C,
	WM_CHANGECBCHAIN = 0x030D,
	WM_HSCROLLCLIPBOARD = 0x030E,
	WM_QUERYNEWPALETTE = 0x030F,
	WM_PALETTEISCHANGING = 0x0310,
	WM_PALETTECHANGED = 0x0311,
	WM_HOTKEY = 0x0312,
	WM_PRINT = 0x0317,
	WM_PRINTCLIENT = 0x0318,
	WM_APPCOMMAND = 0x0319,
	WM_THEMECHANGED = 0x031A,
	WM_CLIPBOARDUPDATE = 0x031D,
	WM_DWMCOMPOSITIONCHANGED = 0x031E,
	WM_DWMNCRENDERINGCHANGED = 0x031F,
	WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320,
	WM_DWMWINDOWMAXIMIZEDCHANGE = 0x0321,
	WM_DWMSENDICONICTHUMBNAIL = 0x0323,
	WM_DWMSENDICONICLIVEPREVIEWBITMAP = 0x0326,
	WM_GETTITLEBARINFOEX = 0x033F,
	WM_HANDHELDFIRST = 0x0358,
	WM_HANDHELDLAST = 0x035F,
	WM_AFXFIRST = 0x0360,
	WM_AFXLAST = 0x037F,
	WM_PENWINFIRST = 0x0380,
	WM_PENWINLAST = 0x038F,
	WM_APP = 0x8000,
	WM_USER = 0x0400,
	WMSZ_LEFT = 1,
	WMSZ_RIGHT = 2,
	WMSZ_TOP = 3,
	WMSZ_TOPLEFT = 4,
	WMSZ_TOPRIGHT = 5,
	WMSZ_BOTTOM = 6,
	WMSZ_BOTTOMLEFT = 7,
	WMSZ_BOTTOMRIGHT = 8
}
//
// WM_NCHITTEST and MOUSEHOOKSTRUCT Mouse Position Codes
//
[Flags]
public enum NCHITTEST
{
	HTERROR = -2,
	HTTRANSPARENT = -1,
	HTNOWHERE = 0,
	HTCLIENT = 1,
	HTCAPTION = 2,
	HTSYSMENU = 3,
	HTGROWBOX = 4,
	HTSIZE = HTGROWBOX,
	HTMENU = 5,
	HTHSCROLL = 6,
	HTVSCROLL = 7,
	HTMINBUTTON = 8,
	HTMAXBUTTON = 9,
	HTLEFT = 10,
	HTRIGHT = 11,
	HTTOP = 12,
	HTTOPLEFT = 13,
	HTTOPRIGHT = 14,
	HTBOTTOM = 15,
	HTBOTTOMLEFT = 16,
	HTBOTTOMRIGHT = 17,
	HTBORDER = 18,
	HTREDUCE = HTMINBUTTON,
	HTZOOM = HTMAXBUTTON,
	HTSIZEFIRST = HTLEFT,
	HTSIZELAST = HTBOTTOMRIGHT,
	HTOBJECT = 19,
	HTCLOSE = 20,
	HTHELP = 21
}

[Flags]
public enum MouseEventDataXButtons : uint
{
	Nothing = 0x00000000,
	XBUTTON1 = 0x00000001,
	XBUTTON2 = 0x00000002
}

[Flags]
public enum MOUSEEVENTF : uint
{
	ABSOLUTE = 0x8000,
	HWHEEL = 0x01000,
	MOVE = 0x0001,
	MOVE_NOCOALESCE = 0x2000,
	LEFTDOWN = 0x0002,
	LEFTUP = 0x0004,
	RIGHTDOWN = 0x0008,
	RIGHTUP = 0x0010,
	MIDDLEDOWN = 0x0020,
	MIDDLEUP = 0x0040,
	VIRTUALDESK = 0x4000,
	WHEEL = 0x0800,
	XDOWN = 0x0080,
	XUP = 0x0100
}

[Flags]
public enum KEYEVENTF : uint
{
	KEYDOWN = 0x0000,
	EXTENDEDKEY = 0x0001,
	KEYUP = 0x0002,
	SCANCODE = 0x0008,
	UNICODE = 0x0004
}

[Flags]
public enum SystemMetric
{
	SM_CXSCREEN = 0,
	SM_CYSCREEN = 1,
}

[Flags]
public enum INPUTTYPE : uint
{
	MOUSE = 0x0,
	KEYBOARD = 0x1,
	HARDWARE = 0x2,
}

[Flags]
public enum VirtualKey : ushort
{
	LBUTTON = 0x01,     //Left mouse button
	RBUTTON = 0x02,     //Right mouse button
	CANCEL = 0x03,      //Control-break processing
	MBUTTON = 0x04,     //Middle mouse button (three-button mouse)
	XBUTTON1 = 0x05,    //Windows 2000
	XBUTTON2 = 0x06,    //Windows 2000
	BACK = 0x08,        //BACKSPACE key
	TAB = 0x09,         //TAB key
	CLEAR = 0x0C,       //CLEAR key
	RETURN = 0x0D,      //ENTER key
	SHIFT = 0x10,       //SHIFT key
	CONTROL = 0x11,     //CTRL key
	MENU = 0x12,        //ALT key
	PAUSE = 0x13,       //PAUSE key
	CAPITAL = 0x14,     //CAPS LOCK key
	KANA = 0x15,        //Input Method Editor (IME) Kana mode
	HANGUL = 0x15,      //IME Hangul mode
	JUNJA = 0x17,       //IME Junja mode
	FINAL = 0x18,       //IME final mode
	HANJA = 0x19,       //IME Hanja mode
	KANJI = 0x19,       //IME Kanji mode
	ESCAPE = 0x1B,      //ESC key
	CONVERT = 0x1C,     //IME convert
	NONCONVERT = 0x1D,  //IME nonconvert
	ACCEPT = 0x1E,      //IME accept
	MODECHANGE = 0x1F,  //IME mode change request

	SPACE = 0x20,       //SPACEBAR
	PRIOR = 0x21,       //PAGE UP key
	NEXT = 0x22,        //PAGE DOWN key
	END = 0x23,         //END key
	HOME = 0x24,        //HOME key
	LEFT = 0x25,        //LEFT ARROW key
	UP = 0x26,          //UP ARROW key
	RIGHT = 0x27,       //RIGHT ARROW key
	DOWN = 0x28,        //DOWN ARROW key
	SELECT = 0x29,      //SELECT key
	PRINT = 0x2A,       //PRINT key
	EXECUTE = 0x2B,     //EXECUTE key
	SNAPSHOT = 0x2C,    //PRINT SCREEN key
	INSERT = 0x2D,      //INS key
	DELETE = 0x2E,      //DEL key
	HELP = 0x2F,        //HELP key
	KEY_0 = 0x30,       //0 key
	KEY_1 = 0x31,       //1 key
	KEY_2 = 0x32,       //2 key
	KEY_3 = 0x33,       //3 key
	KEY_4 = 0x34,       //4 key
	KEY_5 = 0x35,       //5 key
	KEY_6 = 0x36,       //6 key
	KEY_7 = 0x37,       //7 key
	KEY_8 = 0x38,       //8 key
	KEY_9 = 0x39,       //9 key
	KEY_A = 0x41,       //A key
	KEY_B = 0x42,       //B key
	KEY_C = 0x43,       //C key
	KEY_D = 0x44,       //D key
	KEY_E = 0x45,       //E key
	KEY_F = 0x46,       //F key
	KEY_G = 0x47,       //G key
	KEY_H = 0x48,       //H key
	KEY_I = 0x49,       //I key
	KEY_J = 0x4A,       //J key
	KEY_K = 0x4B,       //K key
	KEY_L = 0x4C,       //L key
	KEY_M = 0x4D,       //M key
	KEY_N = 0x4E,       //N key
	KEY_O = 0x4F,       //O key
	KEY_P = 0x50,       //P key
	KEY_Q = 0x51,       //Q key
	KEY_R = 0x52,       //R key
	KEY_S = 0x53,       //S key
	KEY_T = 0x54,       //T key
	KEY_U = 0x55,       //U key
	KEY_V = 0x56,       //V key
	KEY_W = 0x57,       //W key
	KEY_X = 0x58,       //X key
	KEY_Y = 0x59,       //Y key
	KEY_Z = 0x5A,       //Z key
	LWIN = 0x5B,        //Left Windows key (Microsoft Natural keyboard)
	RWIN = 0x5C,        //Right Windows key (Natural keyboard)
	APPS = 0x5D,        //Applications key (Natural keyboard)
	SLEEP = 0x5F,       //Computer Sleep key
	NUMPAD0 = 0x60,     //Numeric keypad 0 key
	NUMPAD1 = 0x61,     //Numeric keypad 1 key
	NUMPAD2 = 0x62,     //Numeric keypad 2 key
	NUMPAD3 = 0x63,     //Numeric keypad 3 key
	NUMPAD4 = 0x64,     //Numeric keypad 4 key
	NUMPAD5 = 0x65,     //Numeric keypad 5 key
	NUMPAD6 = 0x66,     //Numeric keypad 6 key
	NUMPAD7 = 0x67,     //Numeric keypad 7 key
	NUMPAD8 = 0x68,     //Numeric keypad 8 key
	NUMPAD9 = 0x69,     //Numeric keypad 9 key
	MULTIPLY = 0x6A,    //Multiply key
	ADD = 0x6B,         //Add key
	SEPARATOR = 0x6C,   //Separator key
	SUBTRACT = 0x6D,    //Subtract key
	DECIMAL = 0x6E,     //Decimal key
	DIVIDE = 0x6F,      //Divide key
	F1 = 0x70,          //F1 key
	F2 = 0x71,          //F2 key
	F3 = 0x72,          //F3 key
	F4 = 0x73,          //F4 key
	F5 = 0x74,          //F5 key
	F6 = 0x75,          //F6 key
	F7 = 0x76,          //F7 key
	F8 = 0x77,          //F8 key
	F9 = 0x78,          //F9 key
	F10 = 0x79,         //F10 key
	F11 = 0x7A,         //F11 key
	F12 = 0x7B,         //F12 key
	F13 = 0x7C,         //F13 key
	F14 = 0x7D,         //F14 key
	F15 = 0x7E,         //F15 key
	F16 = 0x7F,         //F16 key
	F17 = 0x80,         //F17 key
	F18 = 0x81,         //F18 key
	F19 = 0x82,         //F19 key
	F20 = 0x83,         //F20 key
	F21 = 0x84,         //F21 key
	F22 = 0x85,         //F22 key, (PPC only) Key used to lock device.
	F23 = 0x86,         //F23 key
	F24 = 0x87,         //F24 key
	NUMLOCK = 0x90,     //NUM LOCK key
	SCROLL = 0x91,      //SCROLL LOCK key
	LSHIFT = 0xA0,      //Left SHIFT key
	RSHIFT = 0xA1,      //Right SHIFT key
	LCONTROL = 0xA2,    //Left CONTROL key
	RCONTROL = 0xA3,    //Right CONTROL key
	LMENU = 0xA4,       //Left MENU key
	RMENU = 0xA5,       //Right MENU key
	BROWSER_BACK = 0xA6,    //Windows 2000
	BROWSER_FORWARD = 0xA7,     //Windows 2000
	BROWSER_REFRESH = 0xA8,     //Windows 2000
	BROWSER_STOP = 0xA9,    //Windows 2000
	BROWSER_SEARCH = 0xAA,  //Windows 2000
	BROWSER_FAVORITES = 0xAB,   //Windows 2000
	BROWSER_HOME = 0xAC,    //Windows 2000
	VOLUME_MUTE = 0xAD,     //Windows 2000
	VOLUME_DOWN = 0xAE,     //Windows 2000
	VOLUME_UP = 0xAF,   //Windows 2000
	MEDIA_NEXT_TRACK = 0xB0,    //Windows 2000
	MEDIA_PREV_TRACK = 0xB1,    //Windows 2000
	MEDIA_STOP = 0xB2,  //Windows 2000
	MEDIA_PLAY_PAUSE = 0xB3,    //Windows 2000
	LAUNCH_MAIL = 0xB4,     //Windows 2000
	LAUNCH_MEDIA_SELECT = 0xB5,     //Windows 2000
	LAUNCH_APP1 = 0xB6, //Windows 2000
	LAUNCH_APP2 = 0xB7, //Windows 2000
	OEM_1 = 0xBA,   //Used for miscellaneous characters; it can vary by keyboard.
	OEM_PLUS = 0xBB,    //Windows 2000
	OEM_COMMA = 0xBC,   //Windows 2000
	OEM_MINUS = 0xBD,   //Windows 2000
	OEM_PERIOD = 0xBE,  //Windows 2000
	OEM_2 = 0xBF,       //Used for miscellaneous characters; it can vary by keyboard.
	OEM_3 = 0xC0,       //Used for miscellaneous characters; it can vary by keyboard.
	OEM_4 = 0xDB,       //Used for miscellaneous characters; it can vary by keyboard.
	OEM_5 = 0xDC,       //Used for miscellaneous characters; it can vary by keyboard.
	OEM_6 = 0xDD,       //Used for miscellaneous characters; it can vary by keyboard.
	OEM_7 = 0xDE,       //Used for miscellaneous characters; it can vary by keyboard.
	OEM_8 = 0xDF,       //Used for miscellaneous characters; it can vary by keyboard.
	OEM_102 = 0xE2,     //Windows 2000
	PROCESSKEY = 0xE5,  //Windows 95
	PACKET = 0xE7,      //see Remark in KEYBDINPUT, SendInput, WM_KEYDOWN, and WM_KEYUP
	ATTN = 0xF6,        //Attn key
	CRSEL = 0xF7,       //CrSel key
	EXSEL = 0xF8,       //ExSel key
	EREOF = 0xF9,       //Erase EOF key
	PLAY = 0xFA,        //Play key
	ZOOM = 0xFB,        //Zoom key
	NONAME = 0xFC,      //Reserved
	PA1 = 0xFD,         //PA1 key
	OEM_CLEAR = 0xFE    //Clear key
}

[Flags]
public enum ScanCode : ushort
{
	LBUTTON = 0,
	RBUTTON = 0,
	CANCEL = 70,
	MBUTTON = 0,
	XBUTTON1 = 0,
	XBUTTON2 = 0,
	BACK = 14,
	TAB = 15,
	CLEAR = 76,
	RETURN = 28,
	SHIFT = 42,
	CONTROL = 29,
	MENU = 56,
	PAUSE = 0,
	CAPITAL = 58,
	KANA = 0,
	HANGUL = 0,
	JUNJA = 0,
	FINAL = 0,
	HANJA = 0,
	KANJI = 0,
	ESCAPE = 1,
	CONVERT = 0,
	NONCONVERT = 0,
	ACCEPT = 0,
	MODECHANGE = 0,
	SPACE = 57,
	PRIOR = 73,
	NEXT = 81,
	END = 79,
	HOME = 71,
	LEFT = 75,
	UP = 72,
	RIGHT = 77,
	DOWN = 80,
	SELECT = 0,
	PRINT = 0,
	EXECUTE = 0,
	SNAPSHOT = 84,
	INSERT = 82,
	DELETE = 83,
	HELP = 99,
	KEY_0 = 11,
	KEY_1 = 2,
	KEY_2 = 3,
	KEY_3 = 4,
	KEY_4 = 5,
	KEY_5 = 6,
	KEY_6 = 7,
	KEY_7 = 8,
	KEY_8 = 9,
	KEY_9 = 10,
	KEY_A = 30,
	KEY_B = 48,
	KEY_C = 46,
	KEY_D = 32,
	KEY_E = 18,
	KEY_F = 33,
	KEY_G = 34,
	KEY_H = 35,
	KEY_I = 23,
	KEY_J = 36,
	KEY_K = 37,
	KEY_L = 38,
	KEY_M = 50,
	KEY_N = 49,
	KEY_O = 24,
	KEY_P = 25,
	KEY_Q = 16,
	KEY_R = 19,
	KEY_S = 31,
	KEY_T = 20,
	KEY_U = 22,
	KEY_V = 47,
	KEY_W = 17,
	KEY_X = 45,
	KEY_Y = 21,
	KEY_Z = 44,
	LWIN = 91,
	RWIN = 92,
	APPS = 93,
	SLEEP = 95,
	NUMPAD0 = 82,
	NUMPAD1 = 79,
	NUMPAD2 = 80,
	NUMPAD3 = 81,
	NUMPAD4 = 75,
	NUMPAD5 = 76,
	NUMPAD6 = 77,
	NUMPAD7 = 71,
	NUMPAD8 = 72,
	NUMPAD9 = 73,
	MULTIPLY = 55,
	ADD = 78,
	SEPARATOR = 0,
	SUBTRACT = 74,
	DECIMAL = 83,
	DIVIDE = 53,
	F1 = 59,
	F2 = 60,
	F3 = 61,
	F4 = 62,
	F5 = 63,
	F6 = 64,
	F7 = 65,
	F8 = 66,
	F9 = 67,
	F10 = 68,
	F11 = 87,
	F12 = 88,
	F13 = 100,
	F14 = 101,
	F15 = 102,
	F16 = 103,
	F17 = 104,
	F18 = 105,
	F19 = 106,
	F20 = 107,
	F21 = 108,
	F22 = 109,
	F23 = 110,
	F24 = 118,
	NUMLOCK = 69,
	SCROLL = 70,
	LSHIFT = 42,
	RSHIFT = 54,
	LCONTROL = 29,
	RCONTROL = 29,
	LMENU = 56,
	RMENU = 56,
	BROWSER_BACK = 106,
	BROWSER_FORWARD = 105,
	BROWSER_REFRESH = 103,
	BROWSER_STOP = 104,
	BROWSER_SEARCH = 101,
	BROWSER_FAVORITES = 102,
	BROWSER_HOME = 50,
	VOLUME_MUTE = 32,
	VOLUME_DOWN = 46,
	VOLUME_UP = 48,
	MEDIA_NEXT_TRACK = 25,
	MEDIA_PREV_TRACK = 16,
	MEDIA_STOP = 36,
	MEDIA_PLAY_PAUSE = 34,
	LAUNCH_MAIL = 108,
	LAUNCH_MEDIA_SELECT = 109,
	LAUNCH_APP1 = 107,
	LAUNCH_APP2 = 33,
	OEM_1 = 39,
	OEM_PLUS = 13,
	OEM_COMMA = 51,
	OEM_MINUS = 12,
	OEM_PERIOD = 52,
	OEM_2 = 53,
	OEM_3 = 41,
	OEM_4 = 26,
	OEM_5 = 43,
	OEM_6 = 27,
	OEM_7 = 40,
	OEM_8 = 0,
	OEM_102 = 86,
	PROCESSKEY = 0,
	PACKET = 0,
	ATTN = 0,
	CRSEL = 0,
	EXSEL = 0,
	EREOF = 93,
	PLAY = 0,
	ZOOM = 98,
	NONAME = 0,
	PA1 = 0,
	OEM_CLEAR = 0,
}

[Flags]
public enum SetWindowPosFlags : uint
{
	/// <summary>If the calling thread and the thread that owns the window are attached to different input queues,
	/// the system posts the request to the thread that owns the window. This prevents the calling thread from
	/// blocking its execution while other threads process the request.</summary>
	/// <remarks>SWP_ASYNCWINDOWPOS</remarks>
	AsynchronousWindowPosition = 0x4000,
	/// <summary>Prevents generation of the WM_SYNCPAINT message.</summary>
	/// <remarks>SWP_DEFERERASE</remarks>
	DeferErase = 0x2000,
	/// <summary>Draws a frame (defined in the window's class description) around the window.</summary>
	/// <remarks>SWP_DRAWFRAME</remarks>
	DrawFrame = 0x0020,
	/// <summary>Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to
	/// the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE
	/// is sent only when the window's size is being changed.</summary>
	/// <remarks>SWP_FRAMECHANGED</remarks>
	FrameChanged = 0x0020,
	/// <summary>Hides the window.</summary>
	/// <remarks>SWP_HIDEWINDOW</remarks>
	HideWindow = 0x0080,
	/// <summary>Does not activate the window. If this flag is not set, the window is activated and moved to the
	/// top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter
	/// parameter).</summary>
	/// <remarks>SWP_NOACTIVATE</remarks>
	DoNotActivate = 0x0010,
	/// <summary>Discards the entire contents of the client area. If this flag is not specified, the valid
	/// contents of the client area are saved and copied back into the client area after the window is sized or
	/// repositioned.</summary>
	/// <remarks>SWP_NOCOPYBITS</remarks>
	DoNotCopyBits = 0x0100,
	/// <summary>Retains the current position (ignores X and Y parameters).</summary>
	/// <remarks>SWP_NOMOVE</remarks>
	IgnoreMove = 0x0002,
	/// <summary>Does not change the owner window's position in the Z order.</summary>
	/// <remarks>SWP_NOOWNERZORDER</remarks>
	DoNotChangeOwnerZOrder = 0x0200,
	/// <summary>Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to
	/// the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent
	/// window uncovered as a result of the window being moved. When this flag is set, the application must
	/// explicitly invalidate or redraw any parts of the window and parent window that need redrawing.</summary>
	/// <remarks>SWP_NOREDRAW</remarks>
	DoNotRedraw = 0x0008,
	/// <summary>Same as the SWP_NOOWNERZORDER flag.</summary>
	/// <remarks>SWP_NOREPOSITION</remarks>
	DoNotReposition = 0x0200,
	/// <summary>Prevents the window from receiving the WM_WINDOWPOSCHANGING message.</summary>
	/// <remarks>SWP_NOSENDCHANGING</remarks>
	DoNotSendChangingEvent = 0x0400,
	/// <summary>Retains the current size (ignores the cx and cy parameters).</summary>
	/// <remarks>SWP_NOSIZE</remarks>
	IgnoreResize = 0x0001,
	/// <summary>Retains the current Z order (ignores the hWndInsertAfter parameter).</summary>
	/// <remarks>SWP_NOZORDER</remarks>
	IgnoreZOrder = 0x0004,
	/// <summary>Displays the window.</summary>
	/// <remarks>SWP_SHOWWINDOW</remarks>
	ShowWindow = 0x0040,
}

[Flags]
public enum hWndInsertAfter
{
	HWND_BOTTOM = 1, //Places the window at the bottom of the Z order.If the hWnd parameter identifies a topmost window, the window loses its topmost status and is placed at the bottom of all other windows.
	HWND_NOTOPMOST = -2, //Places the window above all non-topmost windows (that is, behind all topmost windows). This flag has no effect if the window is already a non-topmost window.
	HWND_TOP = 0, //Places the window at the top of the Z order.
	HWND_TOPMOST = -1 // Places the window above all non-topmost windows.The window maintains its topmost position even when it is deactivated.
}