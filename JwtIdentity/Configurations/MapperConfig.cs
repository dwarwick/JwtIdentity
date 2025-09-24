namespace JwtIdentity.Configurations
{
    public class MapperConfig : AutomapperProfile
    {
        public MapperConfig()
        {
            //this.CreateMap<ApplicationUser, UserDto>().ReverseMap();

            _ = CreateMap<BaseModel, BaseViewModel>();
            _ = CreateMap<BaseViewModel, BaseModel>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<ApplicationUser, ApplicationUserViewModel>()
                .ReverseMap()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
                .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore());

            // Updated mapping for ApplicationRole and RoleClaim
            _ = CreateMap<ApplicationRole, ApplicationRoleViewModel>()
                .ForMember(dest => dest.Claims, opt => opt
                    .MapFrom(src => src.Claims));
                    
            _ = CreateMap<ApplicationRoleViewModel, ApplicationRole>();
            
            _ = CreateMap<RoleClaim, RoleClaimViewModel>().ReverseMap();

            // Updated mapping for Feedback
            _ = CreateMap<Feedback, FeedbackViewModel>();
            _ = CreateMap<FeedbackViewModel, Feedback>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<Question, QuestionViewModel>().IncludeAllDerived();

            _ = CreateMap<QuestionViewModel, Question>().IncludeAllDerived()
                .ForMember(x => x.CreatedBy, options => options.Ignore())
                .ForMember(x => x.CreatedById, options => options.Ignore());

            _ = CreateMap<Answer, AnswerViewModel>().IncludeAllDerived();
            _ = CreateMap<AnswerViewModel, Answer>().IncludeAllDerived()
                .ForMember(x => x.CreatedBy, options => options.Ignore())
                .ForMember(x => x.CreatedById, options => options.Ignore());

            foreach (var definition in QuestionDomainRegistry.All)
            {
                _ = CreateMap(definition.QuestionEntityType, definition.Definition.QuestionViewModelType);
                var questionMap = CreateMap(definition.Definition.QuestionViewModelType, definition.QuestionEntityType);
                questionMap.ForMember(nameof(BaseModel.CreatedBy), opt => opt.Ignore());
                questionMap.ForMember(nameof(BaseModel.CreatedById), opt => opt.Ignore());

                _ = CreateMap(definition.AnswerEntityType, definition.Definition.AnswerViewModelType);
                var answerMap = CreateMap(definition.Definition.AnswerViewModelType, definition.AnswerEntityType);
                answerMap.ForMember(nameof(BaseModel.CreatedBy), opt => opt.Ignore());
                answerMap.ForMember(nameof(BaseModel.CreatedById), opt => opt.Ignore());
            }

            _ = CreateMap<SingleChoiceAnswer, SingleChoiceAnswerViewModel>();
            _ = CreateMap<SingleChoiceAnswerViewModel, SingleChoiceAnswer>()
                .ForMember(x => x.CreatedById, options => options.Ignore());

            _ = CreateMap<ChoiceOption, ChoiceOptionViewModel>().ReverseMap()
                .ForMember(dest => dest.MultipleChoiceQuestionId, opt => opt.MapFrom(src => src.MultipleChoiceQuestionId))
                .ForMember(dest => dest.SelectAllThatApplyQuestionId, opt => opt.MapFrom(src => src.SelectAllThatApplyQuestionId));

            _ = CreateMap<Survey, SurveyViewModel>()
                .ForMember(dest => dest.NumberOfResponses, opt => opt.Ignore()); // Don't map this property from Survey model
            
            _ = CreateMap<SurveyViewModel, Survey>();

            _ = CreateMap<PlaywrightLog, PlaywrightLogViewModel>().ReverseMap();
        }
    }
}
