# AutoClickerSub

This software(dll) is control another application.<br>
This software use OpenCVSharp4. see https://github.com/shimat/opencvsharp
<br><br>

# Usage

Search application with TITLE.<br>
IntPtr hWnd = GetWindowHandle(target_window_title)<br>

Make "parameter set" for another methods.(CheckImage,TemplateMatch...)<br>
args arg = SetArgs(IntPtr targetwindowhandle, isRealSize, UseColor, magnification, magnification, threshold)<br>
<br>
Make image's list at 'target_directory_name'<br>
tempatefilelist tmp = GetTemplateFileList(target_directory_name)<br>
<br>
Send Mouse Event to application.<br>
<b>MouseEvent(IntPtr hWnd, MOUSEEVENT, x, y, int wheel, fwKeys)</b><br>
MOUSEEVENT<br>
•MOUSEEVENT.CLICK<br>
•MOUSEEVENT.DBLCLICK<br>
•MOUSEEVENT.WHEEL<br>
•MOUSEEVENT.SWIPE_UP<br>
•MOUSEEVENT.SWIPE_DOWN<br>
•MOUSEEVENT.SWIPE_LEFT<br>
•MOUSEEVENT.SWIPE_RIGHT<br>
fwKeys<br>
•fwKeys.MK_LBUTTON<br>
•fwKeys.MK_RBUTTON<br>
•fwKeys.MK_WHEEL<br>
<br>
Check image is in application.<br>
<b>(bool ret, POINT resized_click_point, POINT REALSIZE_CLICK_POINT) = CheckImage(args, image_file_name)</b><br>
ret = true. found.<br>
resized_click_point. found point.<br>
REALSIZE_CLICK_POINT. found point.<br>
<br>
Check image's(in template file list) is in application.<br>
<b>(bool ret, POINT resized_click_point, POINT REALSIZE_CLICK_POINT, matched_image, matched_file_name, matched_index) = TemplateMatch(args, template_file_list)</b><br>
ret = true. found.<br>
resized_click_point. found point.<br>
REALSIZE_CLICK_POINT. found point.<br>
matched_file_name. First matched file name.<br>
matched_index. index of templatefilelist.<br>
<br>
Get application window size.<br>
<b>RECT rect = GetWindowRectangle(hwnd)</b><br>
<br>
Set application window size.<br>
<b>SetWindowPos(hWnd, rect)</b><br>
<br>

# Install

Copy anyware you like.<br>
<br>
# Sample.
SelectApp.ps1 powershell script for select application. execute this.<br>
SelectApp.xaml GUI<br>
AutoClickerSub.ps1 powershell script for sample usage.<br>
AutoClickerSub.xaml GUI<br>
<br>
# End
