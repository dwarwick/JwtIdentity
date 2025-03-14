﻿namespace JwtIdentity.Configurations
{
    public class MapperConfig : AutomapperProfile
    {
        public MapperConfig()
        {
            //this.CreateMap<ApplicationUser, UserDto>().ReverseMap();

            _ = CreateMap<BaseModel, BaseViewModel>();
            _ = CreateMap<BaseViewModel, BaseModel>()
                .ForMember(x => x.CreatedBy, options => options.Ignore());

            _ = CreateMap<ApplicationUser, ApplicationUserViewModel>().ReverseMap();
            _ = CreateMap<ApplicationRole, ApplicationRoleViewModel>().ReverseMap();
            _ = CreateMap<RoleClaim, RoleClaimViewModel>().ReverseMap();

            _ = CreateMap<Question, QuestionViewModel>().IncludeAllDerived();
            _ = CreateMap<Answer, AnswerViewModel>().IncludeAllDerived();


            _ = CreateMap<QuestionViewModel, Question>().IncludeAllDerived()
                .ForMember(x => x.CreatedBy, options => options.Ignore())
                .ForMember(x => x.CreatedById, options => options.Ignore());

            _ = CreateMap<AnswerViewModel, Answer>().IncludeAllDerived()
                .ForMember(x => x.CreatedBy, options => options.Ignore())
                .ForMember(x => x.CreatedById, options => options.Ignore());


            _ = CreateMap<TextAnswer, TextAnswerViewModel>().ReverseMap();
            _ = CreateMap<TrueFalseAnswer, TrueFalseAnswerViewModel>().ReverseMap();
            _ = CreateMap<MultipleChoiceAnswer, MultipleChoiceAnswerViewModel>();
            _ = CreateMap<MultipleChoiceAnswerViewModel, MultipleChoiceAnswer>()
                .ForMember(x => x.CreatedBy, options => options.Ignore())
                .ForMember(x => x.CreatedById, options => options.Ignore());

            _ = CreateMap<SingleChoiceAnswer, SingleChoiceAnswerViewModel>().ReverseMap();

            _ = CreateMap<ChoiceOption, ChoiceOptionViewModel>().ReverseMap();




            _ = CreateMap<TextQuestion, TextQuestionViewModel>().ReverseMap();
            _ = CreateMap<TrueFalseQuestion, TrueFalseQuestionViewModel>().ReverseMap();

            _ = CreateMap<MultipleChoiceQuestion, MultipleChoiceQuestionViewModel>();
            _ = CreateMap<MultipleChoiceQuestionViewModel, MultipleChoiceQuestion>()
                .ForMember(x => x.CreatedBy, options => options.Ignore())
                .ForMember(x => x.CreatedById, options => options.Ignore());

            _ = CreateMap<Survey, SurveyViewModel>().ReverseMap();

            //this.CreateMap<BaseModel, BaseViewModel>().ReverseMap();

            //this.CreateMap<Forum, ForumViewModel>();

            //this.CreateMap<ForumViewModel, Forum>()
            //    .ForMember(q => q.CreatedBy, options => options.Ignore());

            //this.CreateMap<Post, PostViewModel>();
            //this.CreateMap<PostViewModel, Post>()
            //    .ForMember(x => x.CreatedBy, options => options.Ignore());

            //this.CreateMap<Tag, TagViewModel>();
            //this.CreateMap<TagViewModel, Tag>()
            //    .ForMember(x => x.Id, options => options.Ignore());

            //this.CreateMap<VoteViewModel, Vote>().ReverseMap();
        }
    }
}
