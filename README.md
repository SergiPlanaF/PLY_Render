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

## Data Structure
Each point in the cloud is represented using the following struct:

```csharp
struct point
{
    public float x;
    public float y;
    public float z;
    public int R;
    public int G;
    public int B;
    public int A;
}
```

## Transformation Pipeline
Each vertex in the point cloud is transformed in real time using the current view parameters (yaw, pitch, roll, zoom, and pan offsets). The transformation is applied in the following order:

- Rotation – a composite rotation matrix built from the Euler angles yaw, pitch, and roll.
- Scaling (zoom) – multiplication by the zoom factor.
- Translation (pan) – addition of the gap (pan) offsets Gap_x, Gap_y, Gap_z.

The rotation formulas used are:

```csharp
// x_t, y_t, z_t are the rotated coordinates
x_t = (PointCloud[i].x * (cos_yaw * cos_pitch)) +
      (PointCloud[i].y * ((cos_yaw * sin_pitch * sin_roll) - (sin_yaw * cos_roll))) +
      (PointCloud[i].z * ((cos_yaw * sin_pitch * cos_roll) + (sin_yaw * sin_roll)));

y_t = (PointCloud[i].x * (sin_yaw * cos_pitch)) +
      (PointCloud[i].y * ((sin_yaw * sin_pitch * sin_roll) + (cos_yaw * cos_roll))) +
      (PointCloud[i].z * ((sin_yaw * sin_pitch * cos_roll) - (cos_yaw * sin_roll)));

z_t = (PointCloud[i].x * (-sin_pitch)) +
      (PointCloud[i].y * (cos_pitch * sin_roll)) +
      (PointCloud[i].z * (cos_pitch * cos_roll));

// Apply zoom
x_t = x_t * Local_zoom;
y_t = y_t * Local_zoom;
z_t = z_t * Local_zoom;

// Apply translation (pan)
x_t = Local_Gap_x + x_t;
y_t = Local_Gap_y + y_t;
z_t = Local_Gap_z + z_t; 
```
Note: cos_yaw, sin_yaw, etc. are precomputed from the current yaw, pitch, and roll values. Local_zoom, Local_Gap_x, Local_Gap_y, and Local_Gap_z are the current zoom and pan offsets controlled by the user.

These transformed coordinates are then projected to the screen for rendering.

##  Rendering Approach
The 3D scene is rendered onto a 2D image using a simple Z‑buffer technique:
- A table (array) with the same dimensions as the output image is created to store depth values.
- After applying the transformation pipeline (rotation, zoom, translation), each vertex is projected onto the 2D screen.
- For every pixel position, only the point with the smallest Z distance to the camera is retained and painted. Points behind others are discarded.
  
## Example PLY File
The repository includes a sample PLY file with colors: Cube_Color.ply. You can open it directly from the application for testing.
Additional examples for reference:
```ply
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
0 1.0 0 0 0 0 255
