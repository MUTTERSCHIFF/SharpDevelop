// <file>
//     <copyright see="prj:///doc/copyright.txt">2002-2005 AlphaSierraPapa</copyright>
//     <license see="prj:///doc/license.txt">GNU General Public License</license>
//     <owner name="Denis ERCHOFF" email="d_erchoff@hotmail.com"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using System.Collections;
using System.Drawing;
using System.Drawing.Design;
using System.ComponentModel;
using System.ComponentModel.Design;

using ICSharpCode.Core;
using ICSharpCode.FormsDesigner.Services;
using ICSharpCode.SharpDevelop.Gui;

namespace ICSharpCode.FormsDesigner.Gui
{
	public class SideTabDesigner : AxSideTab
	{
		protected bool loadImages = true;
		IToolboxService toolboxService;
		
		public void CreatedUserControl()
		{
			InitializeComponents();
		}
		
		void InitializeComponents()
		{
		}
		
		protected SideTabDesigner(AxSideBar sideBar, string name, IToolboxService toolboxService) : base(sideBar, name)
		{
			this.toolboxService = toolboxService;
			this.CanSaved = false;
			
			AddDefaultItem();
		}
		
		protected void AddDefaultItem()
		{
			this.Items.Add(new SideTabItemDesigner());
			//Event the user click on an another "control" itemin the current tab
			this.ChoosedItemChanged += new EventHandler(SelectedTabItemChanged);
		}
		
		///<summary>Load an assembly's controls</summary>
		public SideTabDesigner(AxSideBar sideBar, Category category, IToolboxService toolboxService) : this(sideBar, category.Name, toolboxService)
		{
			foreach (ToolComponent component in category.ToolComponents) {
				if (component.IsEnabled) {
					ToolboxItem toolboxItem = new ToolboxItem();
					toolboxItem.TypeName    = component.FullName;
					toolboxItem.Bitmap      = ToolboxProvider.ComponentLibraryLoader.GetIcon(component);
					toolboxItem.DisplayName = component.Name;
					Assembly asm = component.LoadAssembly();
					toolboxItem.AssemblyName = asm.GetName();
					
					this.Items.Add(new SideTabItemDesigner(toolboxItem));
				}
			}
		}
		
		protected void LoadAssembly(string assemblyName)
		{
			Assembly assembly = FindAssembly(assemblyName);
			if (assembly == null) {
				assembly = Assembly.Load(assemblyName);
			}
			
			if (assembly != null) {
				BuildToolboxFromAssembly(assembly);
			}
		}
		
		protected void BuildToolboxFromAssembly(Assembly assembly)
		{
			ArrayList toolboxItems = GetToolboxItemsFromAssembly(assembly);
			foreach (ToolboxItem toolboxItem in toolboxItems) {
				//adding the toolboxitem into the tab with his bitmap and caption
				//loaded from the assembly
				this.Items.Add(new SideTabItemDesigner(toolboxItem));
			}
		}
		
		
		protected Assembly FindAssembly(string assemblyName)
		{
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				if (assembly.GetName().Name == assemblyName) {
					return assembly;
				}
			}
			return null;
		}
		
		protected ArrayList GetToolboxItemsFromAssembly(Assembly assembly)
		{
			ArrayList toolBoxItems = new ArrayList();
			
			Hashtable images = new Hashtable();
			ImageList il = new ImageList();
			// try to load res icon
			string[] imgNames = assembly.GetManifestResourceNames();
			
			foreach (string im in imgNames) {
				if (!im.EndsWith(".resources")) //load resources only to avoid exception on debugging
				{
					try {
						Stream stream = assembly.GetManifestResourceStream(im);
						if (stream != null) {
							Bitmap b = new Bitmap(Image.FromStream(stream, true, false));
							b.MakeTransparent();
							images[im] = il.Images.Count;
							il.Images.Add(b);
							stream.Close();
						}
					} catch (Exception e) {
						LoggingService.Warn("Form Designer: GetToolboxItemsFromAssembly", e);
					}
				}
			}
			Type[] ts = assembly.GetExportedTypes();
			foreach (Type t in ts) {
				if (t.IsPublic && !t.IsAbstract) {
					if (t.IsDefined(typeof(ToolboxItemFilterAttribute), true) || t.IsDefined(typeof(ToolboxItemAttribute), true) || t.IsDefined(typeof(DesignTimeVisibleAttribute), true)  || typeof(System.ComponentModel.IComponent).IsAssignableFrom(t)) {
						
						object[] filterAttrs = t.GetCustomAttributes(typeof(DesignTimeVisibleAttribute), true);
						foreach (DesignTimeVisibleAttribute visibleAttr in filterAttrs) {
							if (!visibleAttr.Visible) {
								goto skip;
							}
						}
						string imageName = String.Concat(t.FullName, ".bmp");
						if (images[imageName] == null) {
							object[] attributes = t.GetCustomAttributes(false);
							if (t.IsDefined(typeof(ToolboxBitmapAttribute), false)) {
								foreach (object attr in attributes) {
									if (attr is ToolboxBitmapAttribute) {
										ToolboxBitmapAttribute toolboxBitmapAttribute = (ToolboxBitmapAttribute)attr;
										Bitmap b = new Bitmap(toolboxBitmapAttribute.GetImage(t));
										b.MakeTransparent();
										il.Images.Add(b);
										images[imageName] =b;
										break;
									}
								}
							}
						}

						ToolboxItem item = new ToolboxItem(t);

						if (images[imageName] != null) {
							try {
								if(images[imageName] is Bitmap)
									item.Bitmap = (Bitmap)images[imageName];
							} catch (Exception ex) {
								MessageService.ShowError(ex, "Exception converting bitmap : " + images[imageName] + " : ");
							}
						}
						toolBoxItems.Add(item);

						skip:;
					}
				}
			}
			return toolBoxItems;
		}
		
		void SelectedTabItemChanged(object sender, EventArgs e)
		{
			AxSideTabItem item = (sender as AxSideTab).ChoosedItem;
			if (item == null) {
				toolboxService.SetSelectedToolboxItem(null);
			} else {
				toolboxService.SetSelectedToolboxItem(item.Tag as ToolboxItem);
			}
		}
	}
}
