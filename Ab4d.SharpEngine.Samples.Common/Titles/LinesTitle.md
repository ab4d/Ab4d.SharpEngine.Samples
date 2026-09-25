# 3D lines

Ab4d.SharpEngine can render many types of 3D lines. 

Line rendering features:
- **Super fast** rendering of 3D lines with any line thickness.
- **Poly-lines** or connected lines are rendered by creating a **mitered or beveled joint** (defined by MiterLimit property).
- Render lines with **dot patterns or stipple lines**.
- Render many **line caps** that define how line starts or ends. Possible line caps: Flat, ArrowAnchor, WideArrowAnchor, WiderArrowAnchor, ShortArrowAnchor, StealthArrowAnchor, ReverseArrowAnchor, ReverseShortArrowAnchor, BoxAnchor, DiamondAnchor.
- Render lines with **different start and end colors**.
- Specify LineThickness in **screen-space** or **world-space** coordinate system.
- Apply **depth bias** so lines can be shown on top of 3D objects.
- Render **hidden** lines or **always visible** lines.
- **Super-sharp** line rendering by using super-sampling.

The folowing SceneNodes can be used to create 3D lines:
- **LineNode** creates a line from the StartPosition to the EndPosition.
- **MultiLineNode** creates multiple lines that can be connected (IsLineStrip is true) or disconnected (IsLineStrip is false).
- **PolyLineNode** creates a poly-line - a line with connected line segments where MiterLimit defines how the connection is rendered (with a mitered or beveled joint).
- **CurveLineNode** creates a line that is defined by a curve type (CurveThroughPoints, BezierCurve, BSpline, NURBSCurve) and control points.
- **CircleLineNode**, **EllipseLineNode** and **EllipseArcLineNode** create circular lines.
- **RectangleNode** creates a rectangle from 4 connected lines.
- **AxisLineNode** creates three perpendicular 3D lines that represent the axes of the current coordinate system.
- **WireCrossNode** creates a wire cross where 3 perpendicular lines cross at the specified position. This can be very useful for marking a specific 3D position.
- **WireBoxNode** creates a box from lines.
- **CornerWireBoxNode** is similar to WireBoxNode, but it shows lines only in the corners.
- **WireGridNode** creates wire grid by defining the major and minor lines that define a 2D grid.