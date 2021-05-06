using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DashDownloader.DashFormat
{
    public class Dash
    {
        private static SegmentTimeline ParseSegmentTimeline(XmlNode segmentTimelineNode)
        {
            return new SegmentTimeline();
        }

        private static SegmentTemplate ParseSegmentTemplate(XmlNode segmentTemplateNode)
        {
            SegmentTimeline segmentTimeline = null;
            foreach (XmlNode child in segmentTemplateNode.ChildNodes)
            {
                if (child.Name == "SegmentTimeline")
                {
                    segmentTimeline = ParseSegmentTimeline(child);
                }
            }

            string initialization = null;
            string media = null;
            int startNumber = -1;
            int timescale = -1;
            foreach (XmlAttribute attribute in segmentTemplateNode.Attributes)
            {
                if (attribute.Name == "initialization")
                {
                    initialization = attribute.Value;
                }
                else if (attribute.Name == "media")
                {
                    media = attribute.Value;
                }
                else if (attribute.Name == "startNumber")
                {
                    startNumber = int.Parse(attribute.Value);
                }
                else if (attribute.Name == "timescale")
                {
                    timescale = int.Parse(attribute.Value);
                }
            }

            SegmentTemplate segmentTemplate = new SegmentTemplate(initialization, media, startNumber, timescale, segmentTimeline);
            return segmentTemplate;
        }

        private static Representation ParseRepresentation(XmlNode representationNode)
        {
            SegmentTemplate segmentTemplate = null;
            foreach (XmlNode child in representationNode.ChildNodes)
            {
                if (child.Name == "SegmentTemplate")
                {
                    segmentTemplate = ParseSegmentTemplate(child);                    
                }
            }

            int id = -1;
            string mimeType = null;
            int bandwidth = -1;
            int width = -1;
            int height = -1;
            foreach (XmlAttribute attribute in representationNode.Attributes)
            {
                if (attribute.Name == "id")
                {
                    id = int.Parse(attribute.Value);
                }
                else if (attribute.Name == "mimeType")
                {
                    mimeType = attribute.Value;
                }
                else if (attribute.Name == "bandwidth")
                {
                    bandwidth = int.Parse(attribute.Value);
                }
                else if (attribute.Name == "width")
                {
                    width = int.Parse(attribute.Value);
                }
                else if (attribute.Name == "height")
                {
                    height = int.Parse(attribute.Value);
                }
            }

            Representation representation = new Representation(id, mimeType, bandwidth, width, height, segmentTemplate);
            return representation;
        }

        private static AdaptationSet ParseAdaptationSet(XmlNode adaptationSetNode)
        {
            List<Representation> representations = new List<Representation>();

            foreach (XmlNode child in adaptationSetNode.ChildNodes)
            {
                if (child.Name == "Representation")
                {
                    Representation representation = ParseRepresentation(child);
                    representations.Add(representation);
                }
            }

            int id = -1;
            string lang = null;
            string contentType = null;
            int maxHeight = -1;
            int maxWidth = -1;
            foreach (XmlAttribute attribute in adaptationSetNode.Attributes)
            {
                if (attribute.Name == "id")
                {
                    id = int.Parse(attribute.Value);
                }
                else if(attribute.Name == "lang")
                {
                    lang = attribute.Value;
                }
                else if (attribute.Name == "contentType")
                {
                    contentType = attribute.Value;
                }
                else if (attribute.Name == "maxHeight")
                {
                    maxHeight = int.Parse(attribute.Value);
                }
                else if (attribute.Name == "maxWidth")
                {
                    maxWidth = int.Parse(attribute.Value);
                }
            }

            AdaptationSet adaptationSet = new AdaptationSet(id, lang, contentType, maxHeight, maxWidth, representations);
            return adaptationSet;
        }

        private static Period ParsePeriod(XmlNode periodNode)
        {
            int id = 0;
            foreach(XmlAttribute attribute in periodNode.Attributes)
            {
                if(attribute.Name == "id")
                {
                    id = int.Parse(attribute.Value);
                }
            }

            List<AdaptationSet> adaptationSets = new List<AdaptationSet>();

            foreach(XmlNode child in periodNode.ChildNodes)
            {
                if (child.Name == "AdaptationSet")
                {
                    AdaptationSet adaptationSet = ParseAdaptationSet(child);
                    adaptationSets.Add(adaptationSet);
                }
            }

            Period period = new Period(id, adaptationSets);
            return period;
        }

        public static Mpd Parse(string dashXmlContent)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(dashXmlContent);

            var mpd = xmlDocument.DocumentElement;

            List<Period> periodList = new List<Period>();

            try
            {
                foreach (XmlNode child in mpd.ChildNodes)
                {
                    if (child.Name == "Period")
                    {
                        Period period = ParsePeriod(child);
                        periodList.Add(period);
                    }
                }
            }
            catch(Exception ex)
            {
                throw new DashParsingException("Check inner exception for details", ex);
            }

            return new Mpd(periodList);
        }
    }
}
