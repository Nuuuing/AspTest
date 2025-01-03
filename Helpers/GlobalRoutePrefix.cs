using Microsoft.AspNetCore.Mvc.ApplicationModels;

public class GlobalRoutePrefix : IApplicationModelConvention {
    private readonly string _prefix;

    public GlobalRoutePrefix(string prefix) {
        _prefix = prefix;
    }

    public void Apply(ApplicationModel application) {
        foreach (var controller in application.Controllers) {
            foreach (var selector in controller.Selectors) {
                if (selector.AttributeRouteModel != null) {
                    // 기존 경로와 전역 프리픽스를 결합
                    selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(
                        new AttributeRouteModel(new Microsoft.AspNetCore.Mvc.RouteAttribute(_prefix)),
                        selector.AttributeRouteModel
                    );
                }
            }
        }
    }
}