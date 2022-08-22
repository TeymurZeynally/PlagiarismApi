using System.IO.Compression;
using System.Text.RegularExpressions;

namespace CodePlagiarismApi.Extractors
{
	public class ZipArchiveExtractor
	{
		public async IAsyncEnumerable<(byte[] Contents, string Name)> Extract(byte[] zipArchive, Regex fileRegex)
		{
			await using var zipRepoStream = new MemoryStream(zipArchive);
			foreach (var zipArchiveEntry in new ZipArchive(zipRepoStream).Entries.Where(x => fileRegex.IsMatch(x.FullName)))
			{
				await using var entryMemoryStream = new MemoryStream();
				await using var entryStream = zipArchiveEntry.Open();
				await entryStream.CopyToAsync(entryMemoryStream).ConfigureAwait(false);
				yield return (entryMemoryStream.ToArray(), $"{zipArchiveEntry.FullName}");
			}
		}
	}
}
