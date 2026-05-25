# PLY ASCII Point Cloud Viewer

A simple 3D point cloud viewer built with C# and Visual Studio 2017.  
Load a point cloud from an ASCII-formatted PLY file containing vertex positions and RGB color, and interactively inspect it by translating, rotating and zooming.

## Features

- **PLY Parser**: Loads standard ASCII-formatted PLY files.
- **3D Visualization**: Renders point clouds or mesh data dynamically.
- **Translation Controls**: Move the model along X, Y, and Z axes.
- **Rotation Controls**: Rotate using Yaw, Pitch, and Roll angles.
- **Zoom Functionality**: Scale the model smoothly in and out.

## Requirements

- Visual Studio 2017 (or later, Community/Professional/Enterprise)
- Windows 7/8/10/11

## Getting Started

1. Clone the repository:
2. Open the project:
   - Launch Visual Studio 2017.
   - Open the `.sln` solution file.
3. **Run the application**:
   - Press `F5` or click **Start** to build and launch the viewer.
4. **Load a file**:
   - Click on **Open File** and select your `.ply` ASCII file.

## Controls

- **Translate**: Use the UI sliders for X, Y, and Z positioning.
- **Rotate**: Use the UI sliders for Yaw, Pitch, and Roll.
- **Zoom**: Use the UI sliders for zoom control.

## Example PLY File
The repository includes a sample PLY file with colors: Cube_Color.ply. You can open it directly from the application for testing.
Additional examples for reference:
'''ply
format ascii 1.0
element vertex 3
property float x
property float y
property float z
property uchar red
property uchar green
property uchar blue
end_header
0 0 0 255 0 0
0 0 1.0 0 0 255 0
0 1.0 0 0 0 0 255'''
