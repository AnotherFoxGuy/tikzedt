﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TikzEdt
{
    /// <summary>
    /// Interaction logic for PdfOverlay.xaml
    /// </summary>
    public partial class PdfOverlay : UserControl
    {

        public delegate void ModifiedEventHandler();
        /// <summary>
        /// This event is called whenever the picture gets modified.
        /// For example, in the handler one should update the code listing
        /// </summary>
        public event ModifiedEventHandler OnModified;

        Tikz_ParseTree _parsetree;
        /// <summary>
        /// The Parse tree currently being displayed is stored in this property.
        /// </summary>
        public Tikz_ParseTree ParseTree
        {
            get { return _parsetree; }
            set { curSel = null; curDragged = null; _parsetree = value; RedrawObjects(); }
        }

        private double lResolution = Consts.ptspertikzunit;
        public double Resolution
        {
            get { return lResolution; }
            set
            {
                if (value > 0)
                    lResolution = value;
            }
        }

        public enum ToolType { move, addvert, addedge, addpath }
        ToolType _tool=ToolType.move;
        public ToolType tool
        {
            get { return _tool; }
            set { _tool = value; }
        }

        public RasterControl rasterizer;

        //List<Control> objects = new List<Control>();

        public PdfOverlay()
        {
            InitializeComponent();
        }

        public void RedrawObjects()
        {
            canvas1.Children.Clear();
            //curDragged = null;
            curSel = null;
            if (ParseTree == null)
                return; // nothing to display

            // set render transform
            //canvas1.RenderTransform

            DrawObject(ParseTree, null);

          /*  foreach (TikzParseItem t in ParseTree.Children)
                if (t is Tikz_Picture)
                    foreach (TikzParseItem tt in (t as Tikz_Picture).Children)
                        if (tt is Tikz_Path)
                            foreach (TikzParseItem ttt in (tt as Tikz_Path).Children)
                                if (ttt is Tikz_XYItem)
                                {
                                    OverlayNode el = new OverlayNode();
                                    el.tikzitem = ttt as Tikz_XYItem;
                                    //Ellipse el = new Ellipse();                                   
                                    el.Stroke = Brushes.Red;
                                    el.Fill = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                    
                                    el.Width = 10;
                                    el.Height = 10;
                                    Canvas.SetLeft(el, Resolution * el.tikzitem.x - el.Width / 2);
                                    Canvas.SetBottom(el, Resolution * el.tikzitem.y - el.Height / 2);  // not quite ok like this???
                                    canvas1.Children.Add(el);
                                }
           * */

            // test 
            /*
            OverlayNode ell = new OverlayNode();
            ell.Stroke = Brushes.Red;
            ell.Fill = Brushes.Red;

            ell.Width = 10;
            ell.Height = 10;
            Canvas.SetLeft(ell, 100);
            Canvas.SetBottom(ell, 100);
            canvas1.Children.Add(ell); */
        }

        public void DrawObject(TikzParseItem tpi, List<OverLayShape> bag)
        {
            //BBGatherer bbg = new BBGatherer();
            if (bag == null)
                bag = new List<OverLayShape>();  // dummy, it is not used
            if (tpi is Tikz_Scope)
            {
                OverlayScope os = new OverlayScope();
                os.tikzitem = tpi as Tikz_Scope;
                foreach (TikzParseItem t in (tpi as TikzContainerParseItem).Children)
                    DrawObject(t, os.children);
                bag.Add(os);
                os.Stroke = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0));
                os.StrokeThickness = 10;
                //os.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0));
                os.AdjustPosition();
                canvas1.Children.Add(os);
            }
            else if (tpi is TikzContainerParseItem)
            {
                foreach (TikzParseItem t in (tpi as TikzContainerParseItem).Children)            
                    DrawObject(t, bag);
            }
            if (tpi is Tikz_XYItem)
            {
                if (!(tpi is Tikz_Coord && (tpi as Tikz_Coord).type == Tikz_CoordType.Named)
                    && !(tpi is Tikz_Node && (tpi as Tikz_Node).coord == null) // TODO: yet to decide whether to draw such things
                    )
                {
                    OverlayNode el = new OverlayNode();
                    el.tikzitem = tpi as Tikz_XYItem;
                    //Ellipse el = new Ellipse();                                   
                    el.Stroke = Brushes.Red;
                    el.Fill = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                    el.AdjustPosition(Resolution);

                    canvas1.Children.Add(el);
                    bag.Add(el);

                    //bbg.Add(new Rect(Canvas.GetLeft(el), Canvas.GetTop(el), el.Width, el.Height));
                }
            }

        }

        OverLayShape curDragged;
        Point DragOrigin; // relative to the currently dragged object
        Point DragOriginC; // Mouse position on Canvas when started dragging
        Point DragOriginO; // bottom left of dragged object

        private void canvas1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IInputElement o = canvas1.InputHitTest(e.GetPosition(canvas1));
            if (o is OverLayShape)
            {
                curDragged = (OverLayShape) o;
                DragOrigin = e.GetPosition(o);
                DragOriginC = e.GetPosition(canvas1);
                DragOriginO = new Point(Canvas.GetLeft(curDragged), Canvas.GetBottom(curDragged));
                //MessageBox.Show(o.ToString());
            }

        }

        private void canvas1_MouseMove(object sender, MouseEventArgs e)
        {
            if (tool == ToolType.move && e.LeftButton == MouseButtonState.Pressed && curDragged != null)
            {
                if (curDragged is OverlayScope)
                {
                    Point p = new Point(e.GetPosition(canvas1).X - DragOriginC.X, e.GetPosition(canvas1).Y - DragOriginC.Y);
                    p = rasterizer.RasterizePixel(p);
                    Canvas.SetLeft(curDragged, DragOriginO.X + p.X);
                    Canvas.SetBottom(curDragged, DragOriginO.Y - p.Y); //hack
                }
                else
                {
                    Point p = new Point(e.GetPosition(canvas1).X - DragOrigin.X + 5, Height - (e.GetPosition(canvas1).Y - DragOrigin.Y) - 5);
                    p = rasterizer.RasterizePixel(p);
                    Canvas.SetLeft(curDragged, p.X - curDragged.Width / 2);
                    //Canvas.SetTop(curDragged, e.GetPosition(canvas1).Y - DragOrigin.Y);
                    //Canvas.SetBottom(curDragged, Canvas.GetTop(curDragged) + 10); //hack

                    Canvas.SetBottom(curDragged, p.Y - curDragged.Height / 2); //hack
                }
            }
        }

        private void canvas1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // adjust position of dragged item (in parsetree) // hack
            if (tool == ToolType.move && curDragged != null)
            {
                Point pp = new Point((Canvas.GetLeft(curDragged)+5) / Resolution, (Canvas.GetBottom(curDragged)+5) / Resolution);
                if (curDragged is OverlayNode)
                {
                    (curDragged as OverlayNode).tikzitem.SetAbsPos(pp);
                    (curDragged as OverlayNode).tikzitem.UpdateText();

                }
                else if (curDragged is OverlayScope)
                {                    
                    Point pdiff = new Point(e.GetPosition(canvas1).X - DragOriginC.X, e.GetPosition(canvas1).Y - DragOriginC.Y);
                    pdiff = rasterizer.RasterizePixel(pdiff);

                    double xs= pdiff.X/Resolution, ys=-pdiff.Y/Resolution;

                    if (xs != 0 || ys != 0)
                    {
                        Tikz_Scope ts = (curDragged as OverlayScope).tikzitem;
                        if (ts.options == null)
                            ts.options = new Tikz_Options();
                        ts.options.SetShiftRel(xs, ys);
                        ts.UpdateText();
                    }
                }
                // update all other item's positions
                foreach (IInputElement o in canvas1.Children)
                {
                    if (o is OverLayShape)
                        (o as OverLayShape).AdjustPosition(Resolution);                    
                }
                curDragged = null;

                OnModified.Invoke();
            }
        }

        OverlayNode _curSel;
        OverlayNode curSel
        {
            get { return _curSel; }
            set
            {
                if (_curSel != null)
                    _curSel.Stroke = Brushes.Red;
                _curSel = value;
                if (_curSel != null)
                    _curSel.Stroke = Brushes.Gold;
            }
        }
        private void canvas1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (tool == ToolType.addvert)
            {
                Point p = new Point(e.GetPosition(canvas1).X, Height - e.GetPosition(canvas1).Y );                
                p = rasterizer.RasterizePixel(p);

                // find next tikzpicture and add
                Tikz_Picture tpict = ParseTree.GetTikzPicture();
                if (tpict != null)
                {
                    Tikz_Node tn = new Tikz_Node();
                    tn.label = "?";
                    tn.coord = new Tikz_Coord();
                    

                    Tikz_Path tp = new Tikz_Path();
                    tp.starttag = @"\node ";
                    tp.endtag = ";";
                    
                    tp.AddChild(tn);
                    tpict.AddChild(tp);
                    tpict.AddChild(new Tikz_Something("\r\n"));

                    // do it here since the coordinate calculation needs the parents' coord. transform
                    tn.SetAbsPos(new Point(p.X / Resolution, p.Y / Resolution)); //hack

                    //tn.UpdateText();
                    tp.UpdateText();
                    //tpict.UpdateText();

                    RedrawObjects();
                    OnModified.Invoke();
                }
            }
            else if (tool == ToolType.addedge)
            {

                IInputElement o = canvas1.InputHitTest(e.GetPosition(canvas1));
                if (!(o is OverlayNode))
                {
                    curSel = null;
                    return;
                }
                OverlayNode n = o as OverlayNode;

                if (curSel == null)
                {
                    curSel=n;
                    return;
                }

                // add an edge curSel to n 
                // find tikzpicture
                Tikz_Picture tpict = ParseTree.GetTikzPicture();
                if (tpict != null)
                {
                    // make sure both nodes involved are nodes
                    if (!(curSel.tikzitem is Tikz_Node) || !(n.tikzitem is Tikz_Node))
                        return; // hack
                    Tikz_Node t1 = curSel.tikzitem as Tikz_Node, t2 = n.tikzitem as Tikz_Node;

                    Tikz_Path tp = new Tikz_Path();
                    tp.starttag = @"\draw ";
                    tp.endtag = ";";

                    Tikz_Coord tc1 = new Tikz_Coord();
                    tc1.type = Tikz_CoordType.Named;
                    Tikz_Coord tc2 = new Tikz_Coord();
                    tc2.type = Tikz_CoordType.Named;

                    tp.AddChild(tc1);
                    tp.AddChild(new Tikz_Something(" edge "));
                    tp.AddChild(tc2);
                    tpict.AddChild(tp);
                    tpict.AddChild(new Tikz_Something("\r\n"));

                    // make sure both nodes have names
                    if (t1.name == "")
                    {
                        t1.SetName(tpict.GetUniqueName());
                        t1.UpdateText();
                    }
                    if (t2.name == "")
                    {
                        t2.SetName(tpict.GetUniqueName());
                        t2.UpdateText();
                    }

                    tc1.nameref = t1.name;
                    tc2.nameref = t2.name;
                    //tc1.UpdateText();
                    tp.UpdateText();
                    //tpict.UpdateText();

                    //RedrawObjects();
                    OnModified.Invoke();
                }

            }
        }
    }

    public abstract class OverLayShape : Shape {
        // in upside down coordinates
        public Rect getBB()
        {
            Rect r = new Rect();
            r.X = Canvas.GetLeft(this);
            r.Y = Canvas.GetBottom(this);
            r.Width = Width;
            r.Height = Height;
            return r;
        }
        public abstract void AdjustPosition(double Resolution);
    }

    public class OverlayScope : OverLayShape
    {
        public List<OverLayShape> children = new List<OverLayShape>();
        public Tikz_Scope tikzitem;

        /// <summary>
        /// Sets the item's position according to its tikzitem's value
        /// </summary>
        public override void AdjustPosition(double Resolution) { AdjustPosition(); }
        public void AdjustPosition() // resolution is ignored here
        {
            Rect r=new Rect(0,0,0,0);
            bool hasone = false;
            foreach (OverLayShape o in children)
            {
                Rect rr = o.getBB();
                if (hasone)
                    r.Union(rr);
                else
                {
                    r = rr;
                    hasone = true;
                }
            }
            if (hasone)
            {
                r.Inflate(20,20);
                //r = new Rect(10, 10, 100, 100);
                Width = r.Width;
                Height = r.Height;
                Canvas.SetLeft(this, r.X);
                Canvas.SetBottom(this, r.Y);
            }
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                // Create a StreamGeometry for describing the shape
                StreamGeometry geometry = new StreamGeometry();
                geometry.FillRule = FillRule.EvenOdd;

                using (StreamGeometryContext context = geometry.Open())
                {
                    InternalDrawNodeGeometry(context);
                }

                // Freeze the geometry for performance benefits
                geometry.Freeze();

                return geometry;
            }
        }

        /// <summary>
        /// Draw a rectangle
        /// </summary>
        /// <param name="context"></param>
        private void InternalDrawNodeGeometry(StreamGeometryContext context)
        {
            context.BeginFigure(new Point(0, 0), true, true);
            context.LineTo(new Point(Width, 0), true, false);
            context.LineTo(new Point(Width, Height), true, false);
            context.LineTo(new Point(0, Height), true, false);

        }

    }

    public class OverlayNode : OverLayShape
    {
        public Tikz_XYItem tikzitem;

        /// <summary>
        /// Sets the item's position according to its tikzitem's value
        /// </summary>
        public override void AdjustPosition(double Resolution)
        {
            Width = 10;
            Height = 10;

            Point p = tikzitem.GetAbsPos();
            Canvas.SetLeft(this, Resolution * p.X - Width / 2);
            Canvas.SetBottom(this, Resolution * p.Y - Height / 2);  // not quite ok like this?
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                // Create a StreamGeometry for describing the shape
                StreamGeometry geometry = new StreamGeometry();
                geometry.FillRule = FillRule.EvenOdd;

                using (StreamGeometryContext context = geometry.Open())
                {
                    InternalDrawNodeGeometry(context);
                }

                // Freeze the geometry for performance benefits
                geometry.Freeze();

                return geometry;
            }
        }

        /// <summary>
        /// Draw an X
        /// </summary>
        /// <param name="context"></param>
        private void InternalDrawNodeGeometry(StreamGeometryContext context)
        {
            context.BeginFigure(new Point(0, 0), true, false);
            context.LineTo(new Point(10, 0), false, false);
            context.LineTo(new Point(10, 10), false, false);
            context.LineTo(new Point(0, 10), false, false);

            context.BeginFigure(new Point(0, 0), false, false);
            context.LineTo(new Point(10, 10), true, true);
            //context.LineTo(new Point(10, 0), true, true);
            
            context.BeginFigure(new Point(10, 0), false, false);
            context.LineTo(new Point(0, 10), true, true);
        }



    }

    

}