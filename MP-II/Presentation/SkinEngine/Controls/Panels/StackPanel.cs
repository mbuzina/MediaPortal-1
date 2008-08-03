#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Drawing;
using MediaPortal.Presentation.DataObjects;
using SlimDX;
using SlimDX.Direct3D9;
using RectangleF = System.Drawing.RectangleF;
using Presentation.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;
using Presentation.SkinEngine.SkinManagement;

namespace Presentation.SkinEngine.Controls.Panels
{
  public class StackPanel : Panel, IScrollInfo
  {
    #region Private fields

    Property _orientationProperty;
    bool _isScrolling;
    float _totalHeight;
    float _totalWidth;
    float _physicalScrollOffsetY = 0;

    #endregion

    #region Ctor

    public StackPanel()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _orientationProperty = new Property(typeof(Orientation), Orientation.Vertical);
    }

    void Attach()
    {
      _orientationProperty.Attach(OnPropertyInvalidate);
    }

    void Detach()
    {
      _orientationProperty.Detach(OnPropertyInvalidate);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      StackPanel p = source as StackPanel;
      Orientation = copyManager.GetCopy(p.Orientation);
      Attach();
    }

    #endregion

    #region Public properties

    public Property OrientationProperty
    {
      get { return _orientationProperty; }
    }

    public Orientation Orientation
    {
      get { return (Orientation)_orientationProperty.GetValue(); }
      set { _orientationProperty.SetValue(value); }
    }

    #endregion

    #region Measure and arrange

    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements.</param>
    public override void Measure(SizeF availableSize)
    {
      //Trace.WriteLine(String.Format("StackPanel.Measure :{0} {1}x{2}", this.Name, (int)availableSize.Width, (int)availableSize.Height));

      float marginWidth = (Margin.Left + Margin.Right) * SkinContext.Zoom.Width;
      float marginHeight = (Margin.Top + Margin.Bottom) * SkinContext.Zoom.Height;
      _desiredSize = new SizeF((float)Width * SkinContext.Zoom.Width, (float)Height * SkinContext.Zoom.Height);
      if (Width <= 0)
        _desiredSize.Width = availableSize.Width - marginWidth;
      if (Height <= 0)
        _desiredSize.Height = availableSize.Height - marginHeight;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }

      _totalHeight = 0.0f;
      _totalWidth = 0.0f;
      SizeF childSize = new SizeF(_desiredSize.Width, _desiredSize.Height);
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible) continue;
        if (Orientation == Orientation.Vertical)
        {
          if (childSize.Width < 0) childSize.Width = 0;
          if (childSize.Height < 0) childSize.Height = 0;
          child.Measure(new SizeF(childSize.Width, 0));
          childSize.Height -= child.DesiredSize.Height;
          _totalHeight += child.DesiredSize.Height;
          child.Measure(new SizeF(childSize.Width, child.DesiredSize.Height));
          if (child.DesiredSize.Width > _totalWidth)
            _totalWidth = child.DesiredSize.Width;
        }
        else
        {
          child.Measure(new SizeF(0, childSize.Height));
          childSize.Width -= child.DesiredSize.Width;
          _totalWidth += child.DesiredSize.Width;

          child.Measure(new SizeF(child.DesiredSize.Width, childSize.Height));
          if (child.DesiredSize.Height > _totalHeight)
            _totalHeight = child.DesiredSize.Height;
        }
      }
      float totalHeight = _totalHeight;
      float totalWidth = _totalWidth;
      if (IsItemsHost)
      {
        if (totalHeight > availableSize.Height && availableSize.Height > 0)
        {
          totalHeight = availableSize.Height;
          _isScrolling = true;
        }
        else
        {
          _isScrolling = false;
          _physicalScrollOffsetY = 0;
        }
      }
      if (Width > 0) totalWidth = (float)Width * SkinContext.Zoom.Width;
      if (Height > 0) totalHeight = (float)Height * SkinContext.Zoom.Height;
      _desiredSize = new SizeF(totalWidth, totalHeight);

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);
      _desiredSize.Width += marginWidth;
      _desiredSize.Height += marginHeight;

      base.Measure(availableSize);
      //Trace.WriteLine(String.Format("StackPanel.measure :{0} {1}x{2} returns {3}x{4}", this.Name, (int)availableSize.Width, (int)availableSize.Height, (int)_desiredSize.Width, (int)_desiredSize.Height));
    }

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("StackPanel.arrange :{0} {1},{2} {3}x{4}", this.Name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));
      RectangleF layoutRect = new RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += Margin.Left * SkinContext.Zoom.Width;
      layoutRect.Y += Margin.Top * SkinContext.Zoom.Height;
      layoutRect.Width -= (Margin.Left + Margin.Right) * SkinContext.Zoom.Width;
      layoutRect.Height -= (Margin.Top + Margin.Bottom) * SkinContext.Zoom.Height;
      ActualPosition = new SlimDX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      switch (Orientation)
      {
        case Orientation.Vertical:
          {
            float totalHeight = 0;
            foreach (FrameworkElement child in Children)
            {
              if (!child.IsVisible) continue;

              PointF location = new PointF(ActualPosition.X, ActualPosition.Y + totalHeight);
              SizeF size = new SizeF(child.DesiredSize.Width, child.DesiredSize.Height);

              //align horizontally 
              if (AlignmentX == AlignmentX.Center)
              {
                location.X += (layoutRect.Width - child.DesiredSize.Width) / 2;
              }
              else if (AlignmentX == AlignmentX.Right)
              {
                location.X = layoutRect.Right - child.DesiredSize.Width;
              }

              child.Arrange(new RectangleF(location, size));
              totalHeight += child.DesiredSize.Height;
            }
          }
          break;

        case Orientation.Horizontal:
          {
            float totalWidth = 0;
            foreach (FrameworkElement child in Children)
            {
              if (!child.IsVisible) continue;
              PointF location = new PointF(ActualPosition.X + totalWidth, ActualPosition.Y);
              SizeF size = new SizeF(child.DesiredSize.Width, child.DesiredSize.Height);

              //align vertically 
              if (AlignmentY == AlignmentY.Center)
              {
                location.Y += (layoutRect.Height - child.DesiredSize.Height) / 2;
              }
              else if (AlignmentY == AlignmentY.Bottom)
              {
                location.Y += layoutRect.Height - child.DesiredSize.Height;
              }

              //ArrangeChild(child, ref location);
              child.Arrange(new RectangleF(location, size));
              totalWidth += child.DesiredSize.Width;
            }
          }
          break;
      }
      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      if (!finalRect.IsEmpty)
      {
        if (_finalRect.Width != finalRect.Width || _finalRect.Height != _finalRect.Height)
          _performLayout = true;
        if (Screen != null) Screen.Invalidate(this);
        _finalRect = new RectangleF(finalRect.Location, finalRect.Size);
      }
      base.Arrange(layoutRect);
    }
    #endregion

    #region Rendering

    protected override void RenderChildren()
    {
      lock (_orientationProperty)
      {
        if (_isScrolling)
        {
          GraphicsDevice.Device.ScissorRect = new System.Drawing.Rectangle((int)ActualPosition.X, (int)ActualPosition.Y, (int)ActualWidth, (int)ActualHeight);
          GraphicsDevice.Device.SetRenderState(RenderState.ScissorTestEnable, true);
          ExtendedMatrix m = new ExtendedMatrix();
          m.Matrix = Matrix.Translation(new Vector3(0, -_physicalScrollOffsetY, 0));
          SkinContext.AddTransform(m);
        }
        foreach (FrameworkElement element in _renderOrder)
        {
          if (!element.IsVisible) continue;
          float posY = (float)(element.ActualPosition.Y - ActualPosition.Y);

          // FIXME Albert78: What should this code do?
          if (_isScrolling)
          {
            // if (posY < _physicalScrollOffsetY) continue;
            // posY -= _physicalScrollOffsetY;
            element.Render();
            // posY += (float)(element.ActualHeight);
            // if (posY > ActualHeight) break;
          }
          else
          {
            element.Render();
          }
        }

        if (_isScrolling)
        {
          GraphicsDevice.Device.SetRenderState(RenderState.ScissorTestEnable, false);
          SkinContext.RemoveTransform();
        }
      }
    }
    #endregion

    protected FrameworkElement FindFocusedElement()
    {
      return (FrameworkElement) FindElement(FocusFinder.Instance);
    }

    #region IScrollInfo Members

    public bool LineDown(PointF point)
    {
      if (this.Orientation == Orientation.Vertical)
      {
        FrameworkElement focusedElement = FindFocusedElement();
        if (focusedElement == null) return false;
        MediaPortal.Control.InputManager.Key key = MediaPortal.Control.InputManager.Key.Down;
        FrameworkElement nextElement = PredictFocusDown(focusedElement, ref key, false);
        if (nextElement == null) return false;
        float posY = (float)((nextElement.ActualPosition.Y + nextElement.ActualHeight) - ActualPosition.Y);
        if ((posY - _physicalScrollOffsetY) < ActualHeight) return false;
        _physicalScrollOffsetY += (nextElement.ActualPosition.Y - focusedElement.ActualPosition.Y);
        nextElement.OnMouseMove((float)nextElement.ActualPosition.X, (float)nextElement.ActualPosition.Y);
        return true;
      }
      return false;
    }

    public bool LineUp(PointF point)
    {
      if (_physicalScrollOffsetY <= 0) return false;
      if (this.Orientation == Orientation.Vertical)
      {
        FrameworkElement focusedElement = FindFocusedElement();
        if (focusedElement == null) return false;
        MediaPortal.Control.InputManager.Key key = MediaPortal.Control.InputManager.Key.Up;
        FrameworkElement prevElement = PredictFocusUp(focusedElement, ref key, false);
        if (prevElement == null) return false;
        if ((prevElement.ActualPosition.Y - ActualPosition.Y) > (_physicalScrollOffsetY)) return false;
        _physicalScrollOffsetY -= (focusedElement.ActualPosition.Y - prevElement.ActualPosition.Y);
        prevElement.OnMouseMove((float)prevElement.ActualPosition.X, (float)prevElement.ActualPosition.Y);
        return true;
      }
      return false;
    }

    public bool LineLeft(PointF point)
    {
      return false;
    }

    public bool LineRight(PointF point)
    {
      return false;
    }

    public bool MakeVisible()
    {
      return false;
    }

    public bool PageDown(PointF point)
    {
      FrameworkElement focusedElement = FindFocusedElement();
      if (focusedElement == null) return false;
      float offsetEnd = (float)(_physicalScrollOffsetY + ActualHeight);
      float y = (float)(focusedElement.ActualPosition.Y - (ActualPosition.Y + _physicalScrollOffsetY));
      if (this.Orientation == Orientation.Vertical)
      {
        while (true)
        {

          if (this.Orientation == Orientation.Vertical)
          {
            focusedElement = FindFocusedElement();
            if (focusedElement == null) return false;
            MediaPortal.Control.InputManager.Key key = MediaPortal.Control.InputManager.Key.Down;
            FrameworkElement nextElement = PredictFocusDown(focusedElement, ref key, false);
            if (nextElement == null) return false;
            float posY = (float)((nextElement.ActualPosition.Y + nextElement.ActualHeight) - ActualPosition.Y);
            if ((posY - _physicalScrollOffsetY) < ActualHeight)
            {
              nextElement.OnMouseMove((float)nextElement.ActualPosition.X, (float)nextElement.ActualPosition.Y);
            }
            else
            {
              float diff = (float)(nextElement.ActualPosition.Y - focusedElement.ActualPosition.Y);
              if (_physicalScrollOffsetY + diff > offsetEnd) break;
              _physicalScrollOffsetY += diff;
              nextElement.OnMouseMove((float)nextElement.ActualPosition.X, (float)nextElement.ActualPosition.Y);
            }
          }
        }
        //OnMouseMove((float)point.X, (float)(ActualPosition.Y + y));
      }

      return true;
    }

    public bool PageLeft(PointF point)
    {
      return false;
    }

    public bool PageRight(PointF point)
    {
      return false;
    }

    public bool PageUp(PointF point)
    {
      FrameworkElement focusedElement = FindFocusedElement();
      if (focusedElement == null) return false;
      float y = (float)(focusedElement.ActualPosition.Y - (ActualPosition.Y + _physicalScrollOffsetY));

      float offsetEnd = (float)(_physicalScrollOffsetY - ActualHeight);
      if (offsetEnd <= 0) offsetEnd = 0;
      if (this.Orientation == Orientation.Vertical)
      {
        while (true)
        {

          if (this.Orientation == Orientation.Vertical)
          {
            focusedElement = FindFocusedElement();
            if (focusedElement == null) return false;
            MediaPortal.Control.InputManager.Key key = MediaPortal.Control.InputManager.Key.Up;
            FrameworkElement prevElement = PredictFocusUp(focusedElement, ref key, false);
            if (prevElement == null) return false;
            if ((prevElement.ActualPosition.Y - ActualPosition.Y) > (_physicalScrollOffsetY))
            {
              prevElement.OnMouseMove((float)prevElement.ActualPosition.X, (float)prevElement.ActualPosition.Y);
            }
            else
            {
              float diff = (float)(focusedElement.ActualPosition.Y - prevElement.ActualPosition.Y);
              if ((_physicalScrollOffsetY - diff) < offsetEnd) break;
              _physicalScrollOffsetY -= diff;
              prevElement.OnMouseMove((float)prevElement.ActualPosition.X, (float)prevElement.ActualPosition.Y);
            }
          }
        }
        //OnMouseMove((float)point.X, (float)(ActualPosition.Y + y));
      }

      return true;
    }

    public double LineHeight
    {
      get { return 1000; }
    }

    public double LineWidth
    {
      get { return 0; }
    }

    public bool ScrollToItemWhichStartsWith(string text, int offset)
    {
      return false;
    }

    public void Home(PointF point)
    {
      FrameworkElement focusedElement = FindFocusedElement();
      if (focusedElement == null) return;
      _physicalScrollOffsetY = 0;
      OnMouseMove((float)ActualPosition.X + 5, (float)(ActualPosition.Y + 5));
    }

    public void End(PointF point)
    {
      FrameworkElement focusedElement = FindFocusedElement();
      if (focusedElement == null) return;
      float offsetEnd = (float)(_totalHeight - ActualHeight);
      float y = (float)(focusedElement.ActualPosition.Y - (ActualPosition.Y + _physicalScrollOffsetY));
      if (this.Orientation == Orientation.Vertical)
      {
        while (true)
        {

          if (this.Orientation == Orientation.Vertical)
          {
            focusedElement = FindFocusedElement();
            if (focusedElement == null) return;
            MediaPortal.Control.InputManager.Key key = MediaPortal.Control.InputManager.Key.Down;
            FrameworkElement nextElement = PredictFocusDown(focusedElement, ref key, false);
            if (nextElement == null) return;
            float posY = (float)((nextElement.ActualPosition.Y + nextElement.ActualHeight) - ActualPosition.Y);
            if ((posY - _physicalScrollOffsetY) < ActualHeight)
            {
              nextElement.OnMouseMove((float)nextElement.ActualPosition.X, (float)nextElement.ActualPosition.Y);
            }
            else
            {
              float diff = (float)(nextElement.ActualPosition.Y - focusedElement.ActualPosition.Y);
              if (_physicalScrollOffsetY + diff > offsetEnd) break;
              _physicalScrollOffsetY += diff;
              nextElement.OnMouseMove((float)nextElement.ActualPosition.X, (float)nextElement.ActualPosition.Y);
            }
          }
        }
        //OnMouseMove((float)point.X, (float)(ActualPosition.Y + y));
      }

      return;
    }

    public void ResetScroll()
    {
      _physicalScrollOffsetY = 0;
    }

    #endregion

    #region Input handling

    public override void OnKeyPressed(ref MediaPortal.Control.InputManager.Key key)
    {
      foreach (UIElement element in Children)
      {
        if (false == element.IsVisible) continue;
        element.OnKeyPressed(ref key);
        if (key == MediaPortal.Control.InputManager.Key.None) return;
      }
    }

    public override void OnMouseMove(float x, float y)
    {
      if (y < ActualPosition.Y) return;
      if (y > ActualHeight + ActualPosition.Y) return;
      foreach (UIElement element in Children)
      {
        if (false == element.IsVisible) continue;
        element.OnMouseMove(x, y + _physicalScrollOffsetY);
      }
    }

    #endregion

    #region Focus prediction

    public override FrameworkElement PredictFocusUp(FrameworkElement focusedFrameworkElement, ref MediaPortal.Control.InputManager.Key key, bool strict)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      foreach (FrameworkElement c in Children)
      {
        if (!c.IsVisible) continue;
        if (!c.IsFocusScope) continue;
        FrameworkElement match = c.PredictFocusUp(focusedFrameworkElement, ref key, strict);
        if (key == MediaPortal.Control.InputManager.Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match.Focusable)
          {
            if (match == focusedFrameworkElement)
            {
              continue;
            }
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedFrameworkElement);
            }
            else
            {
              if (match.ActualPosition.Y + match.ActualHeight >= bestMatch.ActualPosition.Y + bestMatch.ActualHeight)
              {
                float distance = Distance(match, focusedFrameworkElement);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }

    public override FrameworkElement PredictFocusDown(FrameworkElement focusedFrameworkElement, ref MediaPortal.Control.InputManager.Key key, bool strict)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      foreach (FrameworkElement c in Children)
      {
        if (!c.IsVisible) continue;
        if (!c.IsFocusScope) continue;
        FrameworkElement match = c.PredictFocusDown(focusedFrameworkElement, ref key, strict);
        if (key == MediaPortal.Control.InputManager.Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match == focusedFrameworkElement)
          {
            continue;
          }
          if (match.Focusable)
          {
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedFrameworkElement);
            }
            else
            {
              if (match.ActualPosition.Y <= bestMatch.ActualPosition.Y)
              {
                float distance = Distance(match, focusedFrameworkElement);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }

    public override FrameworkElement PredictFocusLeft(FrameworkElement focusedFrameworkElement, ref MediaPortal.Control.InputManager.Key key, bool strict)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      foreach (FrameworkElement c in Children)
      {
        if (!c.IsVisible) continue;
        if (!c.IsFocusScope) continue;
        FrameworkElement match = c.PredictFocusLeft(focusedFrameworkElement, ref key, strict);
        if (key == MediaPortal.Control.InputManager.Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match == focusedFrameworkElement)
          {
            continue;
          }
          if (match.Focusable)
          {
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedFrameworkElement);
            }
            else
            {
              if (match.ActualPosition.X >= bestMatch.ActualPosition.X)
              {
                float distance = Distance(match, focusedFrameworkElement);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }

    public override FrameworkElement PredictFocusRight(FrameworkElement focusedFrameworkElement, ref MediaPortal.Control.InputManager.Key key, bool strict)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      foreach (FrameworkElement c in Children)
      {
        if (!c.IsVisible) continue;
        if (!c.IsFocusScope) continue;
        FrameworkElement match = c.PredictFocusRight(focusedFrameworkElement, ref key, strict);
        if (key == MediaPortal.Control.InputManager.Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match == focusedFrameworkElement)
          {
            continue;
          }
          if (match.Focusable)
          {
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedFrameworkElement);
            }
            else
            {
              if (match.ActualPosition.X <= bestMatch.ActualPosition.X)
              {
                float distance = Distance(match, focusedFrameworkElement);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }

    #endregion

  }
}
