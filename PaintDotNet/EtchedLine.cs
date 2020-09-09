/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution                      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace ContentAwareFill
{
    internal sealed class EtchedLine
        : Control
    {
        private bool selfDrawn = false;
        private Label label;

        public void InitForCurrentVisualStyle()
        {
            // If we are Vista Aero, draw using a GroupBox
            // Else, use the "etched line via a label w/ a border style" trick
            // We would use the "GroupBox" style w/ Luna, except that it wasn't
            // working correctly for some reason.

            switch (UI.VisualStyleClass)
            {
                case VisualStyleClass.Aero:
                    this.selfDrawn = true;
                    break;

                case VisualStyleClass.Luna:
                case VisualStyleClass.Classic:
                case VisualStyleClass.Other:
                    this.selfDrawn = false;
                    break;

                default:
                    throw new InvalidEnumArgumentException();
            }

            if (this.selfDrawn && (this.label != null && this.Controls.Contains(this.label)))
            {
                SuspendLayout();
                this.Controls.Remove(this.label);
                ResumeLayout(false);
                PerformLayout();
                Invalidate(true);
            }
            else if (!this.selfDrawn && (this.label != null || !this.Controls.Contains(this.label)))
            {
                if (this.label == null)
                {
                    this.label = new Label();
                    this.label.BorderStyle = BorderStyle.Fixed3D;
                }

                SuspendLayout();
                this.Controls.Add(this.label);
                ResumeLayout(false);
                PerformLayout();
                Invalidate(true);
            }
        }

        public EtchedLine()
        {
            InitForCurrentVisualStyle();
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            this.TabStop = false;
            SetStyle(ControlStyles.Selectable, false);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            return new Size(proposedSize.Width, 2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.selfDrawn)
            {
                GroupBoxRenderer.DrawGroupBox(e.Graphics, new Rectangle(0, 0, this.Width, 1), GroupBoxState.Normal);
            }

            base.OnPaint(e);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            if (!this.selfDrawn)
            {
                this.label.Bounds = new Rectangle(0, 0, this.Width, this.Height);
            }

            base.OnLayout(levent);
        }
    }
}
