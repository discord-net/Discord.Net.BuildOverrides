CREATE MIGRATION m1pdv62ejjfhkk3uvtoamt6ytzynelz46g7hmwop2ttmhphqlxznla
    ONTO m1scb4cbm4vquwps6zhyqxyckslvhawvky5fpkvnk4grzis4wgegia
{
  ALTER TYPE default::Authorization {
      CREATE REQUIRED PROPERTY name -> std::str {
          SET REQUIRED USING ('n');
          CREATE CONSTRAINT std::exclusive;
      };
  };
};
