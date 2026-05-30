using PptSemanticEditor.Core.Models;

namespace PptSemanticEditor.Core.Interfaces;

public interface ISlideClassifier
{
    SlideClassification Classify(OpenXmlSlideInfo slide);
}
