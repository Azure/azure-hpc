// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Web.Optimization;

namespace Microsoft.Azure.Blast.Web
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                "~/Scripts/jquery-{version}.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                "~/Scripts/bootstrap.js",
                "~/Scripts/respond.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                "~/Content/bootstrap.css",
                "~/Content/site.css",
                "~/Areas/Kablammo/Content/css/kablammo-reduced.css",
                "~/Areas/Kablammo/Content/css/svg.css"));

            bundles.Add(new ScriptBundle("~/bundles/kablammo").Include(
                "~/Areas/Kablammo/Scripts/d3.v3.min.js",
                "~/Areas/Kablammo/Scripts/grapher.js",
                "~/Areas/Kablammo/Scripts/graph.js",
                "~/Areas/Kablammo/Scripts/blast_parser.js",
                "~/Areas/Kablammo/Scripts/blast_results_loader.js",
                "~/Areas/Kablammo/Scripts/auto-loader.js",
                "~/Areas/Kablammo/Scripts/exporter.js",
                "~/Areas/Kablammo/Scripts/image_exporter.js",
                "~/Areas/Kablammo/Scripts/alignment_viewer.js",
                "~/Areas/Kablammo/Scripts/alignment_exporter.js",
                "~/Areas/Kablammo/Scripts/kablammo.js"));
        }
    }
}
