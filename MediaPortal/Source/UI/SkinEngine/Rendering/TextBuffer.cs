#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.Core;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.Rendering
{
  #region Enums
  /// <summary>
  /// An enum used to specify text-alignment.
  /// </summary>
  public enum TextAlignEnum
  {
    /// <summary>
    /// Align text to the left.
    /// </summary>
    Left,
    /// <summary>
    /// Align text to the right.
    /// </summary>
    Right,
    /// <summary>
    /// Cener align text.
    /// </summary>
    Center
  };

  /// <summary>
  /// An enum used to specify text scrolling behaviour.
  /// </summary>
  public enum TextScrollEnum
  {
    /// <summary>
    /// Determine scroll direction based on text size, wrapping and available space.
    /// </summary>
    Auto,

    /// <summary>
    /// No scrolling.
    /// </summary>
    None,

    /// <summary>
    /// Force scrolling text to the left.
    /// </summary>
    Left,

    /// <summary>
    /// Force scrolling text to the right.
    /// </summary>
    Right,

    /// <summary>
    /// Force scrolling text to the top.
    /// </summary>
    Up,

    /// <summary>
    /// Force scrolling text to the bottom.
    /// </summary>
    Down
  };

  #endregion

  public class TextBuffer : IDisposable
  {
    #region Consts

    protected const string EFFECT_FONT = "font";
    protected const string EFFECT_FONT_FADE = "fontfade";

    protected const string PARAM_SCROLL_POSITION = "g_scrollpos";
    protected const string PARAM_TEXT_RECT = "g_textbox";
    protected const string PARAM_COLOR = "g_color";
    protected const string PARAM_ALIGNMENT = "g_alignment";
    protected const string PARAM_FADE_BORDER = "g_fadeborder";

    protected const int FADE_SIZE = 15;
    #endregion

    #region Protected fields

    // Immutable properties
    protected FontAsset _font;
    protected float _fontSize;
    // State
    protected string _text;
    protected bool _textChanged;
    protected SizeF _lastTextSize;
    protected float _lastTextBoxWidth;
    protected bool _lastWrap;
    protected bool _kerning;
    protected int[] _textLines;
    // Rendering
    EffectAsset _effect;
    // Scrolling
    protected Vector2 _scrollPos;
    protected Vector2 _scrollWrapOffset;
    protected DateTime _lastTimeUsed;
    // Vertex buffer
    protected PrimitiveBuffer _buffer = new PrimitiveBuffer();
    
    #endregion

    #region Ctor

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBuffer"/> class.
    /// </summary>
    /// <param name="font">The font to use.</param>
    /// <param name="size">The font size (may be slightly different to the size of <paramref name="font"/>).</param>
    public TextBuffer(FontAsset font, float size)
    {
      SetFont(font, size);
      _kerning = true;
      _lastTimeUsed = DateTime.MinValue;
      _lastTextSize = SizeF.Empty;
      ResetScrollPosition();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBuffer"/> class.
    /// </summary>
    /// <param name="fontName">The name of the font to use.</param>
    /// <param name="size">The font size.</param>
    public TextBuffer(string fontName, float size)
    {
      SetFont(fontName, size);
      _kerning = true;
      _lastTimeUsed = DateTime.MinValue;
      _lastTextSize = SizeF.Empty;
      ResetScrollPosition();
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Gets the font.
    /// </summary>
    public FontAsset Font
    {
      get { return _font; }
    }

    /// <summary>
    /// Gets the font size. This may be slightly different from the size stored in Font.
    /// </summary>
    public float FontSize
    {
      get { return _fontSize; }
    }

    /// <summary>
    /// Gets or sets the text to be rendered.
    /// </summary>
    public string Text
    {
      get { return _text; }
      set
      {
        if (_text == value)
          return;
        _textChanged = true;
        ResetScrollPosition();
        _text = value;
      }
    }

    /// <summary>
    /// Gets the current kerning setting.
    /// </summary>
    public bool Kerning
    {
      get { return _kerning; }
    }

    /// <summary>
    /// Gets the width of a string as if it was rendered by this resource.
    /// </summary>
    /// <param name="text">String to evaluate</param>
    /// <returns>The width of the text in graphics device units (pixels)</returns>
    public float TextWidth(string text)
    {
      return _font.TextWidth(text, _fontSize, _kerning);
    }

    /// <summary>
    /// Gets the height of a number of text lines if rendered by this resource.
    /// </summary>
    /// <param name="lineCount">The number of lines to measure.</param>
    /// <returns>The height of the text in graphics device units (pixels)</returns>
    public float TextHeight(int lineCount)
    {
      return _font.TextHeight(_fontSize, lineCount);
    }

    /// <summary>
    /// Gets the height of text rendered with this resource.
    /// </summary>
    public float LineHeight
    {
      get { return _font.LineHeight(_fontSize); }
    }

    public bool IsAllocated
    {
      get { return _buffer.IsAllocated && _font != null && _font.IsAllocated; }
    }

    public override string ToString()
    {
      return Text;
    }

    #endregion

    #region Public methods
    /// <summary>
    /// Sets the <see cref="FontAsset"/> for this buffer.
    /// </summary>
    /// <param name="font">The <see cref="FontAsset"/>to use.</param>
    /// <param name="size">The size to use for the font.</param>
    public void SetFont(FontAsset font, float size)
    {
      DisposeFont();
      _font = font;
      _font.Deallocated += DisposeBuffer;
      _fontSize = size;
      _textChanged = true;
    }
    /// <summary>
    /// Sets the <see cref="FontAsset"/> for this buffer.
    /// </summary>
    /// <param name="fontName">The name of the font to use.</param>
    /// <param name="size">The size to use for the font.</param>
    public void SetFont(String fontName, float size)
    {
      SetFont(ServiceRegistration.Get<ContentManager>().GetFont(fontName, size), size);
    }

    /// <summary>
    /// Allocates or re-alocates this resource.
    /// </summary>
    /// <param name="boxWidth"></param>
    /// <param name="wrap"></param>
    public void Allocate(float boxWidth, bool wrap)
    {
      if (String.IsNullOrEmpty(_text))
      {
        DisposeBuffer();
        return;
      }
      if (Font == null)
        return;

      // Get text quads
      string[] lines = wrap ? WrapText(boxWidth) : _text.Split(Environment.NewLine.ToCharArray());
      PositionColored2Textured[] verts = _font.CreateText(lines, _fontSize, true, out _lastTextSize, out _textLines);

      // Re-use existing buffer if necessary
      _buffer.Set(ref verts, PrimitiveType.TriangleList);
      
      // Preserve state
      _lastTextBoxWidth = boxWidth;
      _lastWrap = wrap;
      _textChanged = false;
    }

    /// <summary>
    /// Wraps the text of this label to the specified <paramref name="maxWidth"/> and returns the wrapped
    /// text parts. Wrapping is performed on a word-by-word basis.
    /// </summary>
    /// <param name="maxWidth">Maximum available width before the text should be wrapped.</param>
    /// <returns>An array of strings holding the wrapped text lines.</returns>
    public string[] WrapText(float maxWidth)
    {
      if (string.IsNullOrEmpty(_text))
        return new string[0];

      IList<string> result = new List<string>();
      foreach (string para in _text.Split(Environment.NewLine.ToCharArray()))
      {
        int paraLength = para.Length;
        int nextIndex = 0;
        float lineWidth = 0;
        int lineIndex = 0;
        // Split paragraphs into lines that will fit into maxWidth
        while (nextIndex < paraLength)
        {
          int sectionIndex = nextIndex;
          // Iterate over leading spaces
          while (nextIndex < paraLength && char.IsWhiteSpace(para[nextIndex]))
            ++nextIndex;
          if (nextIndex < paraLength)
          {
            // Find length of next word
            int wordIndex = nextIndex;
            while (nextIndex < paraLength && !char.IsWhiteSpace(para[nextIndex]))
              ++nextIndex;
            // Does the word fit into the space?
            // Remember to take into account the additional width required if this was the last word on the line.
            float cx = _font.PartialTextWidth(para, sectionIndex, nextIndex - 1, _fontSize, Kerning);
            float extension = _font.CharWidthExtension(para, nextIndex - 1, _fontSize);

            if (lineWidth + cx + extension > maxWidth)
            {
              // If the word is longer than a line width wrap it by letter
              if (cx + extension > maxWidth)
              {
                // Double check without leading whitespace
                if (_font.PartialTextWidth(para, wordIndex, nextIndex - 1, _fontSize, Kerning) + extension > maxWidth)
                {
                  // Don't wrap whitespace
                  while (sectionIndex < para.Length && char.IsWhiteSpace(para[sectionIndex]))
                  {
                    lineWidth += _font.TextWidth(para, sectionIndex, sectionIndex, _fontSize, Kerning);
                    ++sectionIndex;
                  }
                  // See how many characters of the word we can fit on this line
                  lineWidth += _font.TextWidth(para, sectionIndex, sectionIndex, _fontSize, Kerning);
                  while (lineWidth < maxWidth)
                  {
                    lineWidth += _font.TextWidth(para, sectionIndex, sectionIndex, _fontSize, Kerning);
                    ++sectionIndex;
                  }
                  // Prepare for next line
                  wordIndex = sectionIndex;
                  nextIndex = wordIndex;
                }
              }
              // Start new line						
              if (sectionIndex != lineIndex)
                result.Add(para.Substring(lineIndex, sectionIndex - lineIndex));
              lineIndex = wordIndex;
              lineWidth = _font.TextWidth(para.Substring(wordIndex, nextIndex - wordIndex), _fontSize, Kerning);
            }
            else
              lineWidth += cx;
            if (nextIndex >= paraLength)
            {
              // End of paragraphs
              result.Add(para.Substring(lineIndex, nextIndex - lineIndex));
              lineIndex = nextIndex;
            }
          }
        }
        // If no words found add an empty line to preserve text formatting
        if (lineIndex == 0)
          result.Add("");
      }
      return result.ToArray();
    }

    /// <summary>
    /// Draws this text.
    /// </summary>
    /// <param name="textBox">The text box.</param>
    /// <param name="alignment">The alignment.</param>
    /// <param name="color">The color.</param>
    /// <param name="wrap">If <c>true</c> then text will be word-wrapped to fit the <paramref name="textBox"/>.</param>
    /// <param name="zOrder">A value indicating the depth (and thus position in the visual heirachy) that this element should be rendered at.</param>
    /// <param name="scrollMode">Text scrolling behaviour.</param>
    /// <param name="scrollSpeed">Text scrolling speed in units (pixels at original skin size) per second.</param>
    /// <param name="finalTransform">The final combined layout-/render-transform.</param>
    public void Render(RectangleF textBox, TextAlignEnum alignment, Color4 color, bool wrap, float zOrder,
        TextScrollEnum scrollMode, float scrollSpeed, Matrix finalTransform)
    {
      if (!IsAllocated || wrap != _lastWrap || _textChanged || (wrap && textBox.Width != _lastTextBoxWidth))
      {
        Allocate(textBox.Width, wrap);
        if (!IsAllocated)
          return;
      }

      // Prepare alignment info for shader. X is position offset, Y is multiplyer for line width.
      Vector4 alignParam;
      switch (alignment)
      {
        case TextAlignEnum.Center:
          alignParam = new Vector4(textBox.Width / 2.0f, -0.5f, zOrder, 1.0f);
          break;
        case TextAlignEnum.Right:
          alignParam = new Vector4(textBox.Width, -1.0f, zOrder, 1.0f);
          break;
        //case TextAlignEnum.Left:
        default:
          alignParam = new Vector4(0.0f, 0.0f, zOrder, 1.0f);
          break;
      }
      // Update scrolling
      TextScrollEnum actualScrollMode = scrollMode;
      if (scrollMode != TextScrollEnum.None && _lastTimeUsed != DateTime.MinValue)
        actualScrollMode = UpdateScrollPosition(textBox, scrollMode, scrollSpeed);
      // Do we need to add fading edges?
      Vector4 fadeBorder;
      if (CalculateFadeBorder(actualScrollMode, textBox, alignment, out fadeBorder))
      {
        _effect = ServiceRegistration.Get<ContentManager>().GetEffect(EFFECT_FONT_FADE);
        _effect.Parameters[PARAM_FADE_BORDER] = fadeBorder;
      }
      else
        _effect = ServiceRegistration.Get<ContentManager>().GetEffect(EFFECT_FONT);
      // Render
      _effect.Parameters[PARAM_COLOR] = color;
      _effect.Parameters[PARAM_ALIGNMENT] = alignParam;
      _effect.Parameters[PARAM_SCROLL_POSITION] = new Vector4(_scrollPos.X, _scrollPos.Y, 0.0f, 0.0f);
      _effect.Parameters[PARAM_TEXT_RECT] = new Vector4(textBox.Left, textBox.Top, textBox.Width, textBox.Height);
      DoRender(finalTransform);
      // Because text wraps around before it is complete scrolled off the screen we may need to render a second copy 
      // to create the desired wrapping effect
      if (scrollMode != TextScrollEnum.None)
      {
        if (!float.IsNaN(_scrollWrapOffset.X))
        {
          _effect.Parameters[PARAM_SCROLL_POSITION] = new Vector4(_scrollPos.X + _scrollWrapOffset.X, _scrollPos.Y, 0.0f, 0.0f);
          DoRender(finalTransform);
        }
        else if (!float.IsNaN(_scrollWrapOffset.Y))
        {
          _effect.Parameters[PARAM_SCROLL_POSITION] = new Vector4(_scrollPos.X, _scrollPos.Y + _scrollWrapOffset.Y, 0.0f, 0.0f);
          DoRender(finalTransform);
        }
      }
      _lastTimeUsed = SkinContext.FrameRenderingStartTime;
    }

    #endregion

    #region Protected methods

    protected void DoRender(Matrix finalTransform)
    {
      _effect.StartRender(_font.Texture, finalTransform);
      _buffer.Render(0);
      _effect.EndRender();
    }

    protected TextScrollEnum UpdateScrollPosition(RectangleF textBox, TextScrollEnum mode, float speed)
    {
      float dif = speed * (float) SkinContext.FrameRenderingStartTime.Subtract(_lastTimeUsed).TotalSeconds;

      if (mode == TextScrollEnum.Auto)
      {
        if (_lastWrap && _lastTextSize.Height > textBox.Height)
          mode = TextScrollEnum.Up;
        else if (_textLines.Length == 1 && _lastTextSize.Width > textBox.Width)
          mode = TextScrollEnum.Left;
        else
          return TextScrollEnum.None;
      }

      switch (mode)
      {
        case TextScrollEnum.Left:
          _scrollPos.X -= dif;
          if (_scrollPos.X + _lastTextSize.Width < textBox.Width / 2.0f)
          {
            _scrollWrapOffset.X = _scrollPos.X;
            _scrollPos.X = textBox.Width + 4;
            _scrollWrapOffset.X -= _scrollPos.X;
          }
          else if (_scrollWrapOffset.X + _scrollPos.X + _lastTextSize.Width < 0.0f)
            _scrollWrapOffset.X = float.NaN;
          break;
        case TextScrollEnum.Right:
          _scrollPos.X += dif;
          if (_scrollPos.X > textBox.Width / 2.0f)
          {
            _scrollWrapOffset.X = _scrollPos.X;
            _scrollPos.X = -_lastTextSize.Width - 4;
            _scrollWrapOffset.X -= _scrollPos.X;
          }
          else if (_scrollWrapOffset.X + _scrollPos.X > textBox.Width)
            _scrollWrapOffset.X = float.NaN;
          break;
        case TextScrollEnum.Down:
          _scrollPos.Y += dif;
          if (_scrollPos.Y > textBox.Height / 2.0f)
          {
            _scrollWrapOffset.Y = _scrollPos.Y;
            _scrollPos.Y = -_lastTextSize.Height - 4;
            _scrollWrapOffset.Y -= _scrollPos.Y;
          }
          else if (_scrollWrapOffset.Y + _scrollPos.Y > textBox.Height)
            _scrollWrapOffset.Y = float.NaN;
          break;        
        //case TextScrollEnum.Up:
        default:
          _scrollPos.Y -= dif;
          if (_scrollPos.Y + _lastTextSize.Height < textBox.Height / 2.0f)
          {
            _scrollWrapOffset.Y = _scrollPos.Y;
            _scrollPos.Y = textBox.Height + 4;
            _scrollWrapOffset.Y -= _scrollPos.Y;
          }
          else if (_scrollWrapOffset.Y + _scrollPos.Y + _lastTextSize.Height < 0.0f)
            _scrollWrapOffset.Y = float.NaN;
          break;
      }
      return mode;
    }

    public void ResetScrollPosition()
    {
      _scrollPos = new Vector2(0.0f, 0.0f);
      _scrollWrapOffset = new Vector2(float.NaN, float.NaN);
    }

    #endregion

    #region Protected methods

    protected bool CalculateFadeBorder(TextScrollEnum scrollMode, RectangleF textBox, TextAlignEnum alignment, out Vector4 fadeBorder)
    {
      fadeBorder = new Vector4(0.0001f, 0.0001f, 0.0001f, 0.0001f);
      bool dofade = false;
      if (scrollMode == TextScrollEnum.Left || scrollMode == TextScrollEnum.Right)
      {
        fadeBorder.X = FADE_SIZE; // Fade on left edge
        fadeBorder.Z = FADE_SIZE; // Fade on right edge
        dofade = true;
      }
      else if (_lastTextSize.Width > textBox.Width)
      {
        if (alignment == TextAlignEnum.Right || alignment == TextAlignEnum.Center)
          fadeBorder.X = FADE_SIZE; // Fade on left edge
        if (alignment == TextAlignEnum.Left || alignment == TextAlignEnum.Center)
          fadeBorder.Z = FADE_SIZE; // Fade on right edge
        dofade = true;
      }
      if (scrollMode == TextScrollEnum.Up || scrollMode == TextScrollEnum.Down)
      {
        fadeBorder.Y = FADE_SIZE; // Fade on top edge
        fadeBorder.W = FADE_SIZE; // Fade on bottom edge
        dofade = true;
      }
      else if (_lastTextSize.Height > textBox.Height + _fontSize / 4)
      {
        fadeBorder.W = FADE_SIZE; // Fade on bottom edge
        dofade = true;
      }
      return dofade;
    }

    void DisposeBuffer()
    {
      _buffer.Dispose();
    }

    void DisposeFont()
    {
      if (_font != null)
        _font.Deallocated -= DisposeBuffer;
      _font = null;
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      DisposeBuffer();
      DisposeFont();
    }

    #endregion
  }
}