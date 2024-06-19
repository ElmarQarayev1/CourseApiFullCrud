using JobApplicationLibrary;

namespace JobAppliacationLibrary.UnitTest;

public class ApplicationEvaluateUnitTest
{
    

    [Test]
    public void Application_WithUnderAge_TransferredToAutoRejected()
    {
        //arrange

        var evaluator = new ApplicationEvaluator();
        var form = new JobApplication()
        {
            Application = new Application()
            {
                Age = 17
            }
        };

        //action
        var appResult = evaluator.Evaluate(form);


        //assert

        Assert.AreEqual(appResult, ApplicationResult.AutoAccepted);


    }
 


}
