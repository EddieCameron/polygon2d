# Polygon2D
#### A basic representation of a Polygon in C#, with some common basic utility functions, as well as a component to edit them in the scene

### Scene Editing
![How Use](Documentation/demo.gif?raw=true)

### Helper methods on Polygon2D
- Bounds : Rect
Get a bounding rect that covers the whole polygon

- Translate(Vector2)/Rotate(float)/Scale(Vector2)
Transform the polygon

- Contains(Vector2) : bool
Returns whether the given point is inside the Polygon

- RandomPointWithin() : Vector2
Return a random point from inside the Polygon

- DoesIntersectWithLine( Vector2, Vector2 ) : bool
Returns whether the given line segment crosses the polygon edge

- GetNearestVertex/GetNearestEdge
Returns the closest vertex/edge in the polygon to the given point

## Install
This is set up as a Unity package.
In the Unity Package manager (Window > Package Manager), add this package from the git url. (top left `+` button > Add from GIT url)

## Use
- Polygon2D: In code, create a Polygon2D object from a list of Vector2s. This can be edited and manipulated. 
Note that it is a reference type, so unlike a Vector, shared copies of a Polygon will all be modified if one changes. Use `var newPoly = new Polygon2D( oldPoly )` to make an unlinked copy

- ScenePolygon: A ScenePolygon is just a utility component to make it easy to create a Polygon in the scene view.
After adding a ScenePolygon component, you can edit the vertices manually or in the scene view, much like a PolygonCollider2D
In code you can make an inspector reference to a ScenePolygon type to enable drag and drop, and get the internal polygon out by using `scenePolygon.Polygon`
Like colliders, the internal polygon is stored in local space, to create a world space polygon, use `scenePolygon.GetWorldSpacePolygon()`
