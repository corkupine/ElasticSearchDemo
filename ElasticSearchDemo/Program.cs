using Nest;
using Nest.FactoryDsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearchDemo
{
  public class Employee
  {
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime Birthday { get; set; }
    public List<int> FavoriteNumbers { get; set; }
  }

  class Program
  {
    private static IElasticClient esClient;
    private static string[] names = new string[]
    {
      "Chuck","Frank","Buster","Charlie","Spoony","Creeper","Jimmy","Bob","Splorg"
    };
    private static string[] descWords = new string[]
    {
      "cras","tincidunt","urna","eget","risus","tincidunt","a","convallis","nulla","pharetra","nam","the","sed","sapien","posuere","congue","laoreet","non","ac","nisi"
    };
    private static Guid[] companyIds = new Guid[] 
    {
      Guid.Parse("5CB0021D-1896-4354-89F4-94B350DE700E"), 
      Guid.Parse("CC89DE68-61D9-4F16-93C9-30A3FB3818A7"), 
      Guid.Parse("F99ED90D-679C-4113-B896-2619615F5EA9")
    };
    private static Guid employeeId;
    static void Main(string[] args)
    {
      //Demos

      //1 - Index employees without explicitly creating index
      SetUpElasticSearch();
      IndexNewEmployees(100);
      CountTotalEmployees();
      FilterEmployeesByCompanyId();
      RemoveEmployeesIndex();

      //2 - Index employees after explicitly creating index
      //SetUpElasticSearch();
      //CreateEmployeeIndex();
      //IndexNewEmployees(100);
      //CountTotalEmployees();
      //FilterEmployeesByCompanyId();
      //RemoveEmployeesIndex();

      //3 - Filter employees by favorite numbers
      //SetUpElasticSearch();
      //CreateEmployeeIndex();
      //IndexNewEmployees(100);
      //CountTotalEmployees();
      //FilterEmployeesByFavoriteNumbers(13);
      //RemoveEmployeesIndex();

      //4 - Delete employees by favorite numbers
      //SetUpElasticSearch();
      //CreateEmployeeIndex();
      //IndexNewEmployees(100);
      //CountTotalEmployees();
      //FilterEmployeesByFavoriteNumbers(13);
      //DeleteEmployeesByQueryFavoriteNumbers(13);
      //CountTotalEmployees();
      //RemoveEmployeesIndex();

      //5 - Search and score employees by description text
      //SetUpElasticSearch();
      //CreateEmployeeIndex();
      //IndexNewEmployees(100);
      //CountTotalEmployees();
      //SearchEmployeesByDescriptionText(100);
      //RemoveEmployeesIndex();

      //6 - Update one employee
      //SetUpElasticSearch();
      //CreateEmployeeIndex();
      //IndexNewEmployees(100);
      //CountTotalEmployees();
      //UpdateEmployee();
      //FilterEmployeesByFavoriteNumbers(22);
      //RemoveEmployeesIndex();

      //6 - Get employees with facets
      //SetUpElasticSearch();
      //CreateEmployeeIndex();
      //IndexNewEmployees(100);
      //CountTotalEmployees();
      //GetEmployeesWithFacets();
      //RemoveEmployeesIndex();

      Console.ReadKey();
    }

    private static void GetEmployeesWithFacets()
    {
      var sb = SearchBuilder.Builder();
      sb.Filter(FilterFactory.TermFilter("companyId","CC89DE68-61D9-4F16-93C9-30A3FB3818A7".ToLower()));
      sb.Facet(FacetFactory.TermsFacet("favoriteNumbers").Field("favoriteNumbers"));
      var result = esClient.Search(sb, "employees","employee");
      //var facet = ((TermFacet)result.Facets["favoriteNumbers"]);
      var facetItems = result.FacetItems<TermItem>("favoriteNumbers");
      foreach (var facetItem in facetItems)
      {
        Log(string.Format("Number of matches for facet {0}: {1}", facetItem.Term, facetItem.Count));
      }
    }

    private static void UpdateEmployee()
    { 
      var employee = esClient.Get<Employee>("employees","employee", employeeId.ToString());
      employee.FavoriteNumbers.Add(22);
      esClient.Update(u => u
          .Object(employee)
          .Script("ctx._source = myobj")
          .Params(p => p.Add("myobj", employee))
          .Index("employees")
          .Type("employee")
          .Id(employee.Id.ToString())
          .RetriesOnConflict(5)
          .Refresh());
      Log("Updated employee with new favorite number");
    }

    private static void FilterEmployeesByCompanyId()
    {
      var sb = SearchBuilder.Builder();
      sb.Filter(FilterFactory.TermFilter("companyId", "5CB0021D-1896-4354-89F4-94B350DE700E".ToLower()));
      var result = esClient.Search(sb, "employees");
      Log(string.Format("Employees in Company 5CB0021D:{0}", result.Total));
    }

    private static void SearchEmployeesByDescriptionText(int size)
    {
      var sb = SearchBuilder.Builder();
      sb.Query(QueryFactory.TextQuery("description","urna eget risus")).Size(size);
      var result = esClient.Search(sb, "employees","employee");
      Log("Employees whose Description is closest to 'urna eget risus':");
      result.Hits.Hits.ForEach(h => Log(string.Format("Score: {0}", h.Score.ToString())));
    }

    private static void CountTotalEmployees()
    {
      var sb = SearchBuilder.Builder().Query(QueryFactory.MatchAllQuery());
      var results = esClient.Search(sb, "employees", "employee");
      Log(string.Format("Total employees: {0}", results.Total));
    }

    private static void FilterEmployeesByFavoriteNumbers(int favoriteNumber)
    {
      var sb = SearchBuilder.Builder();
      var filter = FilterFactory.TermFilter("favoriteNumbers", favoriteNumber);
      sb.Filter(filter);
      var results = esClient.Search(sb, "employees", "employee");
      Log(string.Format("Employees whose favorite numbers include {0}: {1}", favoriteNumber, results.Total));
    }

    private static void DeleteEmployeesByQueryFavoriteNumbers(int favoriteNumber)
    {
      esClient.DeleteByQuery<Employee>(q => q.Index("employees").Type("employee").Term(e => e.FavoriteNumbers, favoriteNumber.ToString()));
      Log(string.Format("Deleted employees whose favorite numbers include {0}", favoriteNumber));
    }
    private static void IndexNewEmployees(int numEmployees)
    {
      var idCounter = 0;
      var rnd = new Random();
      
      for (var i = 0; i < numEmployees; i++)
      {
        var description = new StringBuilder();
        for (int c = 0; c < 10; c++) 
        { 
          description.Append(descWords[rnd.Next(descWords.GetUpperBound(0))]);
          description.Append(" ");
        }
        var emp = new Employee
        {
          Birthday = DateTime.Now.AddDays(i),
          CompanyId = companyIds[idCounter],
          Description = description.ToString(),
          FavoriteNumbers = new List<int> { rnd.Next(1, 20), rnd.Next(1, 20), rnd.Next(1, 20), rnd.Next(1, 20), rnd.Next(1, 20), },
          Id = Guid.NewGuid(),
          Name = names[rnd.Next(8)] + " " + names[rnd.Next(8)] 
        };
        esClient.Index(emp, "employees", "employee", emp.Id.ToString(), new IndexParameters { Refresh = true });
        if (idCounter == 2) { idCounter = 0; }
        else { idCounter++; }
        employeeId = emp.Id;
      }
      Log(string.Format("Created {0} employees", numEmployees));
    }

    private static void SetUpElasticSearch()
    {
      esClient = new ElasticClient(new ConnectionSettings("169.254.11.11", 9200));
      ConnectionStatus status = null;
      esClient.TryConnect(out status);
      Log(string.Format("Connection Status: {0}", status.Result));
    }

    private static void CreateEmployeeIndex()
    {
      var settings = new IndexSettings();
      var typeMapping = new TypeMapping("employee");
      var stringNotAnalyzed = new TypeMappingProperty
      {
        Type = "string",
        Index = "not_analyzed"
      };
      //ElasticSearch camel-cases field names
      typeMapping.Properties = new Dictionary<string, TypeMappingProperty>();
      typeMapping.Properties.Add("id", stringNotAnalyzed);
      typeMapping.Properties.Add("companyId", stringNotAnalyzed);

      settings.Mappings.Add(typeMapping);

      settings.NumberOfReplicas = 1;
      settings.NumberOfShards = 5;
      
      //default analyzer is Standard

      var result = esClient.CreateIndex("employees", settings);
      if (!result.OK) 
      { 
        Log("Unable to create and configure employees ElasticSearch index");
        return;
      }
      Log("Employees index created");
    }

    private static void RemoveEmployeesIndex()
    {
      esClient.DeleteIndex("employees");
      Log("Deleted employees index");
    }

    private static void Log(string message)
    {
      message = string.Concat(DateTime.Now.ToLongTimeString(), " => ", message);
      Console.WriteLine(message);
    }
  }

}
