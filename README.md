# AutoClickerSub

This software is released under the MIT License, see [LICENSE.txt](https://github.com/rhanda-dev/AutoClickerSub/blob/master/LICENSE.txt).<br>
This software use [OpenCVSharp4](https://github.com/shimat/opencvsharp)


# Features
- Control another application.
- Send Key.
- Send mouse key.
- Get process list.
- Get Window handle with Title.
- Capture Target application window.
- Check Image on target application window.
    - Using [openCVSharp](https://github.com/shimat/opencvsharp) templatematch.
    - Check some/one file(s) at one time.
    - threshold setting enable.
    - Any image type windows supported.
    - A zooming  Possible.(you can set width, height magnification)
- Click checked image.
    - mouse left key
    - mouse right key
    - mouse wheel up,down
    - mouse swipe up,down,left,right
- Get/Set target application window position.

# Usage

Include Dll your application.<br>
Also use with powershell script about this. <br>

```powershell:sample.ps1
$dllpath = "C:\Anywhere\AutoClickerSub"
Add-Type -Path "$dllpath\AutoClickerSub.dll"
$applist = [AutoClickerSub]::GetProcessList($true) # $true process have WindowText only.
Write-Host "Process List:"
Write-Host $applist
```

<li>Result</li>
<pre>
Process List:
WindowInfo
        [hWnd]:1769888
        [ClassName]:Chrome_WidgetWin_1
        [Text]:SelectApp.ps1 - Visual Studio Code
        [ProcessName]:
        [FullPathName]:C:\xxxx\xxxx\xxxxx\Local\Programs\Microsoft VS Code\Code.exe
        [Rect]:RECT
        left:446
        top:446
        right:1680
        bottom:942
.....
</pre>

# Syntax

- List\<WindowInfo\> GetProcessList(bool _havewindowtext = false)
    - WindowInfo is Class.
<pre>
	public class WindowInfo {
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
	}
</pre>
- IntPtr hWnd = GetWindowHandle(target_window_title)
- args arg = SetArgs(IntPtr _hWnd, bool _isrealsize = default, bool _usehdc = default, bool _usecolor = default, double _dX = 0.5, double _dY = 0.5, double _threshold = 0.8))
    - isRealSize  true is equals to dX =1, dY = 1
    - usehdc true is capture window use Hdc.
    - UseColor true check color image.
    - magnification dX,dY <=n 1.
    - threshold is between 0.8 to 1.
- tempatefilelist tmp = GetTemplateFileList(target_directory_name)
- MouseEvent(IntPtr hWnd, MOUSEEVENT, x, y, int wheel, fwKeys)
<pre>
enum MOUSEEVENT {
	CLICK,
	DBLCLICK,
	WHEEL,
	SWIPE_UP,
	SWIPE_DOWN,
	SWIPE_LEFT,
	SWIPE_RIGHT
}
enum fwKeys {
	MK_LBUTTON,
	MK_RBUTTON,
	MK_WHEEL
}
</pre>

- (bool ret, POINT resized_click_point, POINT REALSIZE_CLICK_POINT) = CheckImage(args, image_file_name)
<pre>
ret = true. found.
resized_click_point. found point.
REALSIZE_CLICK_POINT. found point.
</pre>
- (bool ret, POINT resized_click_point, POINT REALSIZE_CLICK_POINT, matched_image, matched_file_name, matched_index) = TemplateMatch(args, template_file_list)
<pre>
ret = true. found.
resized_click_point. found point.
REALSIZE_CLICK_POINT. found point.
matched_file_name. First matched file name.
matched_index. index of templatefilelist.
</pre>
- RECT rect = GetWindowRectangle(IntPtr hwnd)
- void SetWindowPos(IntPtr hWnd, RECT rect)

# Install

Copy anyware you like.
# Sample.
SelectApp.ps1 powershell script for select application. execute this.<br>
SelectApp.xaml GUI<br>
AutoClickerSub.ps1 powershell script for sample usage.<br>
AutoClickerSub.xaml GUI<br>
<br>
# End
