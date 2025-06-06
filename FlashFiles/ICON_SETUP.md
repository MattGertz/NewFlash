# FlashFiles Icon Setup

I've created the necessary configuration for the FlashFiles icon. Here's what you need to do:

## ✅ Configuration Complete
- ✅ Project file updated with `<ApplicationIcon>FlashFiles.ico</ApplicationIcon>`
- ✅ MainWindow.xaml updated with `Icon="FlashFiles.ico"`
- ✅ Resource inclusion added to project file
- ✅ SVG template created (`FlashFiles.svg`)

## 🔲 Create the Icon File (Required)

**You need to convert the provided SVG to an ICO file:**

1. **Open the `FlashFiles.svg` file** I created in the FlashFiles folder
2. **Convert SVG to ICO using online tools:**
   - Go to: https://convertio.co/svg-ico/
   - Upload `FlashFiles.svg`
   - Download as `FlashFiles.ico`
   - Save it in the FlashFiles project folder (same directory as FlashFiles.csproj)

**Alternative: Use GIMP or Photoshop:**
- Open FlashFiles.svg
- Resize to 48x48 pixels
- Export as ICO format
- Save as `FlashFiles.ico` in the FlashFiles folder

## 🎯 Icon Design
The SVG shows:
- Blue circular background representing the "Flash" theme
- Lightning bolt in gold/yellow for speed
- Small folder/file icon at bottom for file synchronization

## 🔧 After Creating the ICO File
1. Place `FlashFiles.ico` in `c:\Users\mattge\source\repos\NewFlash\FlashFiles\`
2. Build the project: the icon will appear in:
   - Window title bar
   - System tray (if minimized)
   - Taskbar
   - .exe file icon in Explorer
