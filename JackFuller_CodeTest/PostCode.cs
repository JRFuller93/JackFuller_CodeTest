//The class used for breaking down the data pulled from the PostCode.IO API
public class Postcodeobject
{
    public int status { get; set; }
    public Result[] result { get; set; }
}

public class Result 
{
    public string query { get; set; }
    public PostcodeData result { get; set; }
}

public class PostcodeData 
{
    public string postcode { get; set; }
    public int eastings { get; set; }
    public int northings { get; set; }
    public string parliamentary_constituency { get; set; }
}