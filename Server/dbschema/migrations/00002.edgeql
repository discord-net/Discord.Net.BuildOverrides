CREATE MIGRATION m1scb4cbm4vquwps6zhyqxyckslvhawvky5fpkvnk4grzis4wgegia
    ONTO m17xfvjennpwad3bx544bbntxfcpr6vjcgrnirnv7xoxbmy4zkdxqa
{
  ALTER TYPE default::Authorization {
      ALTER PROPERTY key {
          CREATE CONSTRAINT std::exclusive;
      };
  };
};
