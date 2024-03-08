// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExampleMvcRazorPagesApp.Pages.SmartPaste
{
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
            ViewData["Title"] = "Smart Paste";
        }
    }
}
