using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Drawing = System.Drawing;
using GH = Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel.Attributes;
using System.Threading;
using Point = System.Drawing.Point;

//REF: https://github.com/ladybug-tools/ladybug-grasshopper-dotnet/blob/master/src/Ladybug.Grasshopper/Component/Ladybug_ImageViewer.cs
namespace Brain.UtilComps
{
    public class ImageViewer : GH_Component
    {

        public List<Bitmap> Bitmaps = new List<Bitmap>();
        public int currentBitmapIndex = 0;

        public Bitmap Bitmap;

        public double Scale = 1;

        public ImageViewer()
          : base("Image Viewer", "Viewer",
              "Preview bitmap objects\n\nSimplified from:\nhttps://github.com/ladybug-tools/ladybug-grasshopper-dotnet",
              "Brain", "Utils")
        { }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Bitmap", "BMP", "System.Drawing.Bitmap object", GH_ParamAccess.list);
            pManager[0].Optional = true;
            pManager[0].MutableNickName = false;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }

        protected override void BeforeSolveInstance()
        {
            base.BeforeSolveInstance();

            this.Bitmaps = new List<Bitmap>();
            this.Bitmap = null;
            this.Message = null;
            this.currentBitmapIndex = 0;
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<GH_ObjectWrapper> inputs = new List<GH_ObjectWrapper>();
            if (!DA.GetDataList(0, inputs)) return;

            this.Bitmaps = inputs.Where(x => x.Value is Bitmap).Select(x => x.Value as Bitmap).ToList();

            if (Bitmaps.IsNullOrEmpty())
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid images");
                return;
            }

            //current img to be shown
            this.Bitmap = this.Bitmaps[this.currentBitmapIndex];
        }

        public override Guid ComponentGuid => new Guid("{051359AB-778E-4FF1-BF7C-5111CE5D84DC}");

        public override void CreateAttributes()
        {
            var newAttri = new ImageFromPathAttrib(this);
            newAttri.mouseNavClickEvent += OnMouseNavClickEvent;
            m_attributes = newAttri;
        }

        private void OnMouseNavClickEvent(object sender, bool clickedRightButton)
        {
            if (!clickedRightButton)
                if (currentBitmapIndex > 0)
                    currentBitmapIndex--;
                else
                    currentBitmapIndex = this.Bitmaps.Count - 1;
            else
                if (currentBitmapIndex < this.Bitmaps.Count - 1)
                currentBitmapIndex++;
            else
                currentBitmapIndex = 0;

            this.Message = string.Format("({0}/{1})", currentBitmapIndex + 1, this.Bitmaps.Count);
            this.Bitmap = this.Bitmaps[currentBitmapIndex];

            this.Attributes.ExpireLayout();
            GH.Instances.ActiveCanvas.Document.NewSolution(false);
        }
    }

    public class ImageFromPathAttrib : GH_ComponentAttributes
    {
        private const int rawSize = 320;

        private const int TopOffset = 60;
        private const int NavButtonSize = 20;
        private int BtmOffset = 0;

        private double scale;


        private Bitmap imgBitmap;

        private Graphics MyGraphics;
        private ImageViewer ViewOwner;

        public ImageFromPathAttrib(ImageViewer owner)
            : base(owner)
        {
            this.imgBitmap = owner.Bitmap;
            this.scale = owner.Scale;
            this.ViewOwner = (ImageViewer)this.Owner;
        }

        protected override void Layout()
        {

            this.scale = this.ViewOwner.Scale;
            this.imgBitmap = this.ViewOwner.Bitmap;

            if (this.ViewOwner.Bitmaps.Count > 1)
                this.BtmOffset = 30;
            else
                this.BtmOffset = 0;

            if (this.imgBitmap == null)
                this.Bounds = GetBounds(this.Pivot, new SizeF(rawSize, rawSize - TopOffset), TopOffset, BtmOffset, 1);
            else
            {
                var size = new SizeF(rawSize, (float)((double)rawSize / (double)this.imgBitmap.Width * (double)this.imgBitmap.Height));
                this.Bounds = GetBounds(this.Pivot, size, TopOffset, BtmOffset, scale);
            }

            //locate the inputs outputs
            RectangleF inputRect = new RectangleF(Pivot, new SizeF(10f, 54f));
            inputRect.X += Owner.Params.InputWidth;

            RectangleF outRect = new RectangleF(Pivot, new SizeF(10f, 54f));
            outRect.X += Bounds.Width - Owner.Params.OutputWidth - 10;

            LayoutInputParams(Owner, inputRect);
            LayoutOutputParams(Owner, outRect);
        }

        private RectangleF GetBounds(PointF location, SizeF imgSizeXY, int topOffset, int btmOffset, double scale)
        {
            RectangleF rec = new RectangleF();
            rec.Location = location;
            rec.Width = imgSizeXY.Width * (float)scale;
            rec.Height = imgSizeXY.Height * (float)scale + topOffset + btmOffset;
            rec.Inflate(2f, 2f);

            return (RectangleF)GH_Convert.ToRectangle(rec);
        }

        private RectangleF GetImgBounds(RectangleF bounds, int topOffset, int btmOffset)
        {
            RectangleF rec = bounds;
            rec.Y += topOffset;
            rec.Height = rec.Height - topOffset - btmOffset;

            rec.Inflate(-2f, -2f);
            return rec;
        }

        private RectangleF GetNavBounds(RectangleF imgBound, int btmOffset, int size, bool rightSide)
        {
            RectangleF rec = imgBound;

            if (rightSide)
                rec.X += (rec.Width - size - 10);
            else
                rec.X += 10;

            rec.Y += (rec.Height + (btmOffset - size) / 2);
            rec.Width = size;
            rec.Height = size;

            return rec;
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            MyGraphics = graphics;

            if (channel == GH_CanvasChannel.Objects)
                if (this.imgBitmap != null)
                {
                    DisplayImg(this.imgBitmap);
                    DisplayNav();
                }
                else
                    DisplayDefaultComponent();
        }

        private void DisplayNav()
        {
            if (this.BtmOffset == 0) return;

            RectangleF recImg = GetImgBounds(this.Bounds, TopOffset, BtmOffset);
            Pen pen = new Pen(new SolidBrush(Color.White));
            RectangleF recNavLeft = GetNavBounds(recImg, BtmOffset, NavButtonSize, false);
            RectangleF recNavRight = GetNavBounds(recImg, BtmOffset, NavButtonSize, true);

            MyGraphics.DrawEllipse(pen, recNavLeft);
            MyGraphics.DrawEllipse(pen, recNavRight);

            MyGraphics.DrawString("◄", new Font("Arial", 10), new SolidBrush(Color.White), recNavLeft.X, recNavLeft.Y + 3);
            MyGraphics.DrawString("►", new Font("Arial", 10), new SolidBrush(Color.White), recNavRight.X + 3, recNavRight.Y + 3);
        }

        private void DisplayImg(Bitmap inBitmap)
        {
            RectangleF rec = GetImgBounds(this.Bounds, TopOffset, BtmOffset);

            MyGraphics.DrawImage(imgBitmap, rec);
        }

        private void DisplayDefaultComponent()
        {
            //reset the comonent
            imgBitmap = null;
            this.scale = 1;
            this.Owner.Message = null;

            RectangleF rec = GetImgBounds(Bounds, TopOffset, BtmOffset);

            Pen pen = new Pen(Color.Gray, 3);
            SolidBrush myBrush = new SolidBrush(Color.Gray);

            Font standardFont = GH_FontServer.Standard; //29
            //Font standardFont4kScreen = new Font(standardFont.Name, 4); //15
            Font standardFontAdjust = GH_FontServer.NewFont(standardFont, (float)Math.Round(120M / standardFont.Height));

            StringFormat myFormat = new StringFormat();

            MyGraphics.FillRectangle(myBrush, Rectangle.Round(rec));
            MyGraphics.DrawString("Please use a valid bitmap object.\nSystem.Drawing.Bitmap", standardFontAdjust, Brushes.White, new Point((int)rec.X + 12, (int)rec.Y + ((int)rec.Width * 2 / 3) + 10), myFormat);
            myBrush.Color = Color.White;
            MyGraphics.FillRectangle(myBrush, (int)rec.X, (int)rec.Y, (int)rec.Width, (int)rec.Width * 2 / 3);

            myBrush.Dispose();
            myFormat.Dispose();
        }

        public delegate void NavClick_Handler(object sender, bool clickedRightButton);

        private NavClick_Handler MouseNavClickEvent;

        public event NavClick_Handler mouseNavClickEvent
        {
            add
            {
                NavClick_Handler buttonHandler = MouseNavClickEvent;
                NavClick_Handler comparand;
                do
                {
                    comparand = buttonHandler;
                    buttonHandler = Interlocked.CompareExchange(ref this.MouseNavClickEvent, (NavClick_Handler)Delegate.Combine(comparand, value), comparand);
                }
                while (buttonHandler != comparand);
            }
            remove
            {
                NavClick_Handler buttonHandler = MouseNavClickEvent;
                NavClick_Handler comparand;
                do
                {
                    comparand = buttonHandler;
                    buttonHandler = Interlocked.CompareExchange(ref this.MouseNavClickEvent, (NavClick_Handler)Delegate.Remove(comparand, value), comparand);
                }
                while (buttonHandler != comparand);
            }
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && imgBitmap != null)
            {
                RectangleF recImg = GetImgBounds(this.Bounds, TopOffset, BtmOffset);
                RectangleF recNavLeft = GetNavBounds(recImg, BtmOffset, NavButtonSize, false);
                RectangleF recNavRight = GetNavBounds(recImg, BtmOffset, NavButtonSize, true);

                if (recNavLeft.Contains(e.CanvasLocation))
                    //Left Nav click
                    this.MouseNavClickEvent(this, false);
                else if (recNavRight.Contains(e.CanvasLocation))
                    //Right Nav click
                    this.MouseNavClickEvent(this, true);
            }
            return base.RespondToMouseDown(sender, e);
        }
    }
}
