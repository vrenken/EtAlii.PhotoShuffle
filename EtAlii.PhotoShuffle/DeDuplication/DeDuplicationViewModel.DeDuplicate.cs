namespace EtAlii.PhotoShuffle
{
    using System.IO;
    using System.Threading.Tasks;

    public partial class DeDuplicationViewModel 
    {
        private Task DeDuplicate()
        {
            return DeDuplicate(true);
        }

        private Task DeDuplicate(bool commit)
        {
            var process = new DeDuplicationProcess();
            return process.Execute(Source, Target, Output, OnlyMatchSimilarSizedFiles, commit);
        }

        private bool CanDeDuplicate()
        {
            var prerequisitesMet = 
                    ! string.IsNullOrWhiteSpace(Source) &
                    ! string.IsNullOrWhiteSpace(Target) &
                    Source != Target &
                    Directory.Exists(Source) &
                    Directory.Exists(Target);
            return prerequisitesMet;
        }
    }
}