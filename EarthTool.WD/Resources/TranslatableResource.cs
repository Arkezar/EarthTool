namespace EarthTool.WD.Resources
{
  public abstract class TranslatableResource : Resource
  {
    public string TranslationId
    {
      get;
    }

    protected TranslatableResource(string filename, (uint, uint, uint) fileInfo, string translationId, byte[] data = null) : base(filename, fileInfo, data)
    {
      TranslationId = translationId;
    }


    public override string ToString()
    {
      return $"{base.ToString()} TransaltionID: {TranslationId}";
    }
  }
}
