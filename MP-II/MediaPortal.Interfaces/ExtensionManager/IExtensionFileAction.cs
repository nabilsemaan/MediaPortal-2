﻿#region Copyright (C) 2007-2008 Team MediaPortal

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

using System.Collections.Generic;

namespace MediaPortal.Core.ExtensionManager
{
  public interface IExtensionFileAction
  {
    /// <summary>
    /// Return a zip entry for a FileItem.
    /// </summary>
    /// <param name="item">The file item.</param>
    /// <returns></returns>
    string GetZipEntry(IExtensionFileItem item);

    string GetDirEntry(IExtensionFileItem item);

    string Description();

    List<string> Param1List();
    
    List<string> Param2List();

    List<string> Param3List();
    
    bool Install(object holder, IExtensionFileItem fileItem, IExtensionPackage pak);

    bool UnInstall(object holder, IExtensionFileItem fileItem, IExtensionPackage pak);
  }
}
