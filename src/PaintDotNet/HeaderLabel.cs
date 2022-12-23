/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution                      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ContentAwareFill
{
    internal sealed class HeaderLabel
        : Control
    {
        private const TextFormatFlags textFormatFlags =
            TextFormatFlags.Default |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.HidePrefix |
            TextFormatFlags.NoPadding |
            TextFormatFlags.NoPrefix |
            TextFormatFlags.SingleLine;

        private int leftMargin = 2;
        private int rightMargin = 8;

        private EtchedLine etchedLine;

        [DefaultValue(8)]
        public int RightMargin
        {
            get
            {
                return this.rightMargin;
            }

            set
            {
                this.rightMargin = value;
                PerformLayout();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.etchedLine?.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            PerformLayout();
            Refresh();
            base.OnFontChanged(e);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            PerformLayout();
            Refresh();
            base.OnTextChanged(e);
        }

        public HeaderLabel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.Selectable, false);
            UI.InitScaling(null);
            this.TabStop = false;
            this.ForeColor = SystemColors.Highlight;
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;

            SuspendLayout();
            this.etchedLine = new EtchedLine();
            this.Controls.Add(this.etchedLine);
            this.Size = new Size(144, 14);
            ResumeLayout(false);
        }

        private int GetPreferredWidth(Size proposedSize)
        {
            Size textSize = GetTextSize();
            return this.leftMargin + textSize.Width;
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            return new Size(Math.Max(proposedSize.Width, GetPreferredWidth(proposedSize)), GetTextSize().Height);
        }

        private Size GetTextSize()
        {
            string textToUse = string.IsNullOrEmpty(this.Text) ? " " : this.Text;

            Size size = TextRenderer.MeasureText(textToUse, this.Font, this.ClientSize, textFormatFlags);

            if (string.IsNullOrEmpty(this.Text))
            {
                size.Width = 0;
            }

            return size;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            Size textSize = GetTextSize();

            int lineLeft = (string.IsNullOrEmpty(this.Text) ? 0 : this.leftMargin) + textSize.Width + (string.IsNullOrEmpty(this.Text) ? 0 : 1);
            int lineRight = this.ClientRectangle.Right - this.rightMargin;

            this.etchedLine.Size = this.etchedLine.GetPreferredSize(new Size(lineRight - lineLeft, 1));
            this.etchedLine.Location = new Point(lineLeft, (this.ClientSize.Height - this.etchedLine.Height) / 2);

            base.OnLayout(levent);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (SolidBrush backBrush = new SolidBrush(this.BackColor))
            {
                e.Graphics.FillRectangle(backBrush, e.ClipRectangle);
            }

            Size textSize = GetTextSize();
            Color textColor = this.BackColor != DefaultBackColor ? this.ForeColor : SystemColors.WindowText;
            TextRenderer.DrawText(e.Graphics, this.Text, this.Font, new Point(this.leftMargin, 0), textColor, textFormatFlags);

            base.OnPaint(e);
        }
    }
}
