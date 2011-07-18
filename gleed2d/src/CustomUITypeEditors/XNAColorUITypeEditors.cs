using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Microsoft.Xna.Framework.Graphics;


namespace CustomUITypeEditors
{

    /// <summary>
    /// Class extending the <see cref="ColorEditor"/> which adds the capability to change the 
    /// alpha value of the color. For use on a property of type: Microsoft.Xna.Framework.Graphics.Color.
    /// </summary>
    public class XNAColorUITypeEditor : ColorEditor
    {
        /// <summary>
        /// Wrapper for the private ColorUI class nested within <see cref="ColorEditor"/>.
        /// It publishes its internals via reflection and adds a <see cref="TrackBar"/> to
        /// adjust the alpha value.
        /// </summary>
        public class ColorUIWrapper
        {
            private Control _control;
            private MethodInfo _startMethodInfo;
            private MethodInfo _endMethodInfo;
            private PropertyInfo _valuePropertyInfo;
            private TrackBar _tbAlpha;
            private Label _lblAlpha;
            private bool _inSizeChange = false;

            /// <summary>
            /// Creates a new instance.
            /// </summary>
            /// <param name="colorEditor">The editor this instance belongs to.</param>
            public ColorUIWrapper(XNAColorUITypeEditor colorEditor)
            {
                Type colorUiType = typeof(ColorEditor).GetNestedType("ColorUI", BindingFlags.CreateInstance | BindingFlags.NonPublic);
                ConstructorInfo constructorInfo = colorUiType.GetConstructor(new Type[] { typeof(ColorEditor) });
                _control = (Control)constructorInfo.Invoke(new object[] { colorEditor });

                _control.BackColor = System.Drawing.SystemColors.Control;

                Panel alphaPanel = new Panel();
                alphaPanel.BackColor = System.Drawing.SystemColors.Control;
                alphaPanel.Dock = DockStyle.Right;
                alphaPanel.Width = 28;
                _control.Controls.Add(alphaPanel);

                _tbAlpha = new TrackBar();
                _tbAlpha.Orientation = Orientation.Vertical;
                _tbAlpha.Dock = DockStyle.Fill;
                _tbAlpha.TickStyle = TickStyle.None;
                _tbAlpha.Maximum = byte.MaxValue;
                _tbAlpha.Minimum = byte.MinValue;
                _tbAlpha.ValueChanged += new EventHandler(OnTrackBarAlphaValueChanged);
                alphaPanel.Controls.Add(_tbAlpha);

                _lblAlpha = new Label();
                _lblAlpha.Text = "0";
                _lblAlpha.Dock = DockStyle.Bottom;
                _lblAlpha.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                alphaPanel.Controls.Add(_lblAlpha);

                _startMethodInfo = _control.GetType().GetMethod("Start");
                _endMethodInfo = _control.GetType().GetMethod("End");
                _valuePropertyInfo = _control.GetType().GetProperty("Value");

                _control.SizeChanged += new EventHandler(OnControlSizeChanged);
            }

            /// <summary>
            /// The control to be shown when a color is edited.
            /// The concrete type is ColorUI which is privately hidden
            /// within System.Drawing.Design.
            /// </summary>
            public Control Control
            {
                get { return _control; }
            }

            /// <summary>
            /// Gets the edited color with applied alpha value.
            /// </summary>
            public object Value
            {
                get
                {
                    object result = _valuePropertyInfo.GetValue(_control, new object[0]);
                    if (result is System.Drawing.Color) result = System.Drawing.Color.FromArgb(_tbAlpha.Value, (System.Drawing.Color)result);
                    return result;
                }
            }

            public void Start(IWindowsFormsEditorService service, object value)
            {
                if (value is System.Drawing.Color) _tbAlpha.Value = ((System.Drawing.Color)value).A;
                _startMethodInfo.Invoke(_control, new object[] { service, value });
            }

            public void End()
            {
                _endMethodInfo.Invoke(_control, new object[0]);
            }

            private void OnControlSizeChanged(object sender, EventArgs e)
            {
                if (_inSizeChange) return;
                try
                {
                    _inSizeChange = true;
                    TabControl tabControl = (TabControl)_control.Controls[0];
                    System.Drawing.Size size = tabControl.TabPages[0].Controls[0].Size;
                    //Rectangle rectangle = tabControl.GetTabRect(0);
                    _control.Size = new System.Drawing.Size(_tbAlpha.Width + size.Width, size.Height + tabControl.GetTabRect(0).Height);
                }
                finally
                {
                    _inSizeChange = false;
                }
            }

            private void OnTrackBarAlphaValueChanged(object sender, EventArgs e)
            {
                _lblAlpha.Text = _tbAlpha.Value.ToString();
            }
        }

        private ColorUIWrapper _colorUI;

        public XNAColorUITypeEditor() 
        { 
        }

        /// <summary>
        /// Edits the given value.
        /// </summary>
        /// <param name="context">Context infromation.</param>
        /// <param name="provider">Service provider.</param>
        /// <param name="value">Value to be edited.</param>
        /// <returns>An edited value.</returns>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (provider != null)
            {
                IWindowsFormsEditorService service = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                if (service == null) return value;

                if (_colorUI == null) _colorUI = new ColorUIWrapper(this);

                Color xnacolor = (Color)value;
                _colorUI.Start(service, System.Drawing.Color.FromArgb(xnacolor.A, xnacolor.R, xnacolor.G, xnacolor.B));

                service.DropDownControl(_colorUI.Control);
                if ((_colorUI.Value != null) /*&& (((Color)_colorUI.Value) != Color.Empty)*/)
                {
                    //value = _colorUI.Value;
                    System.Drawing.Color rescolor = (System.Drawing.Color)_colorUI.Value;
                    value = new Color(rescolor.R, rescolor.G, rescolor.B, rescolor.A);
                }
                _colorUI.End();
            }
            return value;
        }

        
        public override void PaintValue(PaintValueEventArgs e)
        {
            if (e.Value is Color && ((Color)e.Value).A <= byte.MaxValue)
            {
                Color xnacolor = (Color)e.Value;
                System.Drawing.Color syscolor = System.Drawing.Color.FromArgb(xnacolor.A, xnacolor.R, xnacolor.G, xnacolor.B);

                int oneThird = e.Bounds.Width / 3;
                using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.DarkGray))
                {
                    e.Graphics.FillRectangle(brush, new System.Drawing.Rectangle(e.Bounds.X + 1, e.Bounds.Y + 1, 4, 4));
                    e.Graphics.FillRectangle(brush, new System.Drawing.Rectangle(e.Bounds.X + 9, e.Bounds.Y + 1, 4, 4));
                    e.Graphics.FillRectangle(brush, new System.Drawing.Rectangle(e.Bounds.X + 17, e.Bounds.Y + 1, 2, 4));
                    
                    e.Graphics.FillRectangle(brush, new System.Drawing.Rectangle(e.Bounds.X + 5, e.Bounds.Y + 5, 4, 4));
                    e.Graphics.FillRectangle(brush, new System.Drawing.Rectangle(e.Bounds.X + 13, e.Bounds.Y + 5, 4, 4));

                    e.Graphics.FillRectangle(brush, new System.Drawing.Rectangle(e.Bounds.X + 1, e.Bounds.Y + 9, 4, 3));
                    e.Graphics.FillRectangle(brush, new System.Drawing.Rectangle(e.Bounds.X + 9, e.Bounds.Y + 9, 4, 3));
                    e.Graphics.FillRectangle(brush, new System.Drawing.Rectangle(e.Bounds.X + 17, e.Bounds.Y + 9, 2, 3));

                }
                using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(syscolor))
                {
                    e.Graphics.FillRectangle(brush, new System.Drawing.Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height - 1));
                }
            }

            if (e.Value is System.Drawing.Color)
            {
                base.PaintValue(e);
            }

        }

    }





















}
