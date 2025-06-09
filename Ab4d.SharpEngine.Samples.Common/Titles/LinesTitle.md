# 3D lines

Ab4d.SharpEngine can render many types of 3D lines. The basic types are:
- **LineNode** creates a line from the StartPosition to the EndPosition.
- **MultiLineNode** creates multiple lines that can be connected (IsLineStrip is true) or disconnected (IsLineStrip is false).
- **PolyLineNode** creates a poly-line - a line with connected line segments where MiterLimit defines how the connection is rendered (with mitered or beveled joint).
- **CurveLineNode** creates a line that is defined by a curve type (CurveThroughPoints, BezierCurve, BSpline, NURBSCurve) and control points.
- **CircleLineNode**, **EllipseLineNode** and **EllipseArcLineNode** create circular lines.
- **RectangleNode** creates a rectangle from 4 connected lines.
- **WireCrossNode** creates a wire cross where 3 perpendicular lines cross at the specified position. This can be very useful for marking a specific 3D position.
- **WireBoxNode** creates a box from lines.
- **WireGridNode** creates wire grid by defining the major and minor lines that define a 2D grid.

There are two major line types: 
1. **poly-lines** or connected lines that are connected by a mitered or beveled joint
2. **disconnected lines** or multi-lines where each line is a separate line with its own line ending.

By default each line ends with a rectangular shape, but it is also possible to render many different **line caps** like arrows, diamonds and other shapes.

Usually lines are hidden when they are behind other 3D objects. But it is also possible to render **hidden** lines to show the inner structure of a 3D object.