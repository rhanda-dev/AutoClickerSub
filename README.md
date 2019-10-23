# AutoClickerSub
This software is released under the MIT License, see LICENSE.txt.<br>
This software use OpenCVSharp4. see https://github.com/shimat/opencvsharp
<br><br>
This software(dll) is a control another application.<br>
<br><br>
# How To Use.
<br>
## Search application with TITLE
IntPtr hWnd = GetWindowHandle(target_window_title)

## Make parameter set for another methods.(CheckImage,TemplateMatch...)
args arg = SetArgs(IntPtr targetwindowhandle, isRealSize, UseColor, magnification, magnification, threshold)

## Make image's list at  'target_directory_name'
tempatefilelist tmp =  GetTemplateFileList(target_directory_name)

## Send Mouse Event to application.
MOUSEEVENT : [MOUSEEVENT]::CLICK,DBLCLICK,WHEEL,SWIPE_UP,SWIPE_DOWN,SWIPE_LEFT,SWIPE_RIGHT<br>
fwKeys : [fwKeys]::MK_LBUTTON, MK_RBUTTON, MK_WHEEL<br>
MouseEvent(IntPtr hWnd, MOUSEEVENT, x, y, int wheel, fwKeys)<br>

## Check image is in application.
(bool ret, POINT resized_click_point, POINT REALSIZE_CLICK_POINT) =	CheckImage(args, image_file_name)<br>
ret = true. found.<br>
resized_click_point. found point.<br>
REALSIZE_CLICK_POINT. found point.<br>

## Check image's(in template file list) is in application.
(bool ret, POINT resized_click_point, POINT REALSIZE_CLICK_POINT, matched_image, matched_file_name, matched_index) = TemplateMatch(args, template_file_list)<br>
ret = true. found.<br>
resized_click_point. found point.<br>
REALSIZE_CLICK_POINT. found point.<br>
matched_file_name. First matched file name.<br>
matched_index. index of templatefilelist.<br>

## Get application window size.
RECT rect =	GetWindowRectangle(hwnd)<br>

## Set application window size.
SetWindowPos(hWnd, rect)<br>

# Sample.
SelectApp.ps1  powershell script for select application. execute this.<br>
SelectApp.xaml GUI<br>
AutoClickerSub.ps1 powershell script for sample usage.<br>
AutoClickerSub.xaml GUI<br>
<br>
END.
