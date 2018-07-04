﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Plato.Internal.Abstractions.Extensions;

namespace Plato.Internal.Layout.TagHelpers
{

    public enum CardType
    {
        Default,
        Primary,
        Success,
        Info,
        Warning,
        Danger
    }


    [RestrictChildren("card-title", "card-body", "card-footer")]
    public class CardTagHelper : TagHelper
    {
        
        public CardType Type { get; set; }

        //public LocalizedHtmlString Title { get; set; }

        [ViewContext] // inform razor to inject
        public ViewContext ViewContext { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {

            var cardContext = new CardContext();
            context.Items.Add(typeof(CardContext), cardContext);

            await output.GetChildContentAsync();
            
            output.TagName = "div";
            output.Attributes.Add("class", $"card card-{Type.ToString().ToLower()}");
            
            // title
            if (cardContext.Title != null)
            {
                //var h3 = new TagBuilder("h6");
                //h3.AddCssClass("card-title");
                //h3.InnerHtml.AppendHtml(cardContext.Title);

                var panelTitle = new TagBuilder("div");
                panelTitle.AddCssClass("card-header");
                panelTitle.InnerHtml.AppendHtml(cardContext.Title);

                output.Content.AppendHtml(panelTitle);
            }

            // panel body
            if (cardContext.Body != null)
            {
                var panelBody = new TagBuilder("div");
                panelBody.AddCssClass("card-body");
                panelBody.InnerHtml.AppendHtml(cardContext.Body);

                output.Content.AppendHtml(panelBody);
            }

            // panel footer
            if (cardContext.Footer != null)
            {
                var panelFooter = new TagBuilder("div");
                panelFooter.AddCssClass("card-footer");
                panelFooter.InnerHtml.AppendHtml(cardContext.Footer);

                output.Content.AppendHtml(panelFooter);
            }

        }

        //public IHtmlContent Build(IHtmlContent innerContent)
        //{

        //    var builder = new HtmlContentBuilder();

        //    var htmlContentBuilder = builder.AppendHtml("<div class=\"card\">");

        //    if (Title != null)
        //    {
        //        htmlContentBuilder
        //            .AppendHtml("<div class=\"card-header\">")
        //            .AppendHtml(Title.Value)
        //            .AppendHtml("</div>");
        //    }
      
        //    htmlContentBuilder
        //        .AppendHtml("<div class=\"card-body\">")
        //        .AppendHtml(innerContent)
        //        .AppendHtml("</div>")
        //        .AppendHtml("</div>");
            

        //    return htmlContentBuilder.ToHtmlString();


        //}


    }
}